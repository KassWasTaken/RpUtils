using Dalamud.Game.ClientState.Objects.SubKinds;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using RpUtils.Models;
using System.Collections.Generic;
using System;
using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using RpUtils.Services;
using System.Timers;
using Dalamud.Game.Gui.Dtr;
using FFXIVClientStructs.FFXIV.Client.Game.Housing;

namespace RpUtils.Controllers
{
    public class SonarController : IDisposable
    {
        private Configuration configuration;
        private ConnectionService connectionService;
        private ExcelSheet<Map> Maps { get; set; }
        private ExcelSheet<OnlineStatus> OnlineStatuses { get; set; }
        private DtrBarEntry dtrBarEntry;

        private Timer positionCheckTimer;
        private Vector3 lastReportedPosition = Vector3.Zero;
        private bool lastReportedInHousing = false;
        private const float DistanceThreshold = 5.0f;
        private const int PositionCheckInterval = 10000;
        private bool WasRoleplaying = false;

        
        public SonarController(Configuration configuration, ConnectionService connectionService) 
        {
            this.configuration = configuration;
            this.connectionService = connectionService;

            Maps = DalamudContainer.DataManager.GetExcelSheet<Map>()!;
            OnlineStatuses = DalamudContainer.DataManager.GetExcelSheet<OnlineStatus>()!;
            
            // Adding our subscriber for when the SonarEnabled configuration changes
            this.configuration.OnSonarEnabledChanged += OnConfigChangedHandler;
            this.configuration.OnUtilsEnabledChanged += OnConfigChangedHandler;
            this.connectionService.OnConnectionChange += OnConfigChangedHandler;

            // Setting up timer for how often we check our position
            positionCheckTimer = new Timer(PositionCheckInterval);
            positionCheckTimer.Elapsed += CheckAndSubmitPlayerPosition;
            positionCheckTimer.AutoReset = true;

            // Setting up our DTR bar entry
            dtrBarEntry =  DalamudContainer.DtrBar.Get("RP Sonar");
            UpdateDtr();
            this.configuration.OnShowSonarDtrChanged += OnShowDtrChangedHandler;
            
        }

        // Handler for our config change listener, we're just going to kick off the toggle
        public void OnConfigChangedHandler(object sender, EventArgs e)
        {
            ToggleSonar();
            SetDtrText();
        }

        // Determines whether to toggle sonar on or off. We need the SonarEnabled, UtilsEnabled, and the ConnectionService to have a connection
        public void ToggleSonar()
        {
            if (this.configuration.SonarEnabled && this.configuration.UtilsEnabled && this.connectionService.Connected)
            {
                DalamudContainer.PluginLog.Debug("Enabling Sonar");
                EnableSonar();
            }
            else
            {
                DalamudContainer.PluginLog.Debug("Disabling Sonar");
                DisableSonar();
            }
        }

        private void EnableSonar()
        {
            DalamudContainer.Lifecycle.RegisterListener(Dalamud.Game.Addon.Lifecycle.AddonEvent.PostRefresh, "AreaMap", OnOpenMap);
            positionCheckTimer.Enabled = true;
        }

        private void DisableSonar()
        {
            DalamudContainer.Lifecycle.UnregisterListener(Dalamud.Game.Addon.Lifecycle.AddonEvent.PostRefresh, "AreaMap", OnOpenMap);
            positionCheckTimer.Enabled = false;
            ClearMapMarkers();
        }

        private void OnOpenMap(AddonEvent type, AddonArgs args)
        {
            FindNearbyRp();
        }

        public async Task FindNearbyRp()
        {
            var player = DalamudContainer.ClientState.LocalPlayer;
            var map = GetSelectedMap();

            try
            {
                DalamudContainer.PluginLog.Debug($"Searching for RP in {player.CurrentWorld.Id}:{map.Id.RawString}");
                var positions = await connectionService.InvokeHubMethodAsync<List<Position>>("GetPlayersInWorldMap", player.CurrentWorld.Id, map.Id.RawString);

                DalamudContainer.PluginLog.Debug($"Positions found: {positions.Count}");

                OpenRpMap(positions);
            }
            catch (Exception ex)
            {
                DalamudContainer.PluginLog.Debug($"Error fetching data from server: {ex}");
            }
        }

        

        // TODO Do we need to be careful about clearing map markers? Can we remove specifically the ones we've added?
        private unsafe void ClearMapMarkers()
        {
            var agent = AgentMap.Instance();
            agent->ResetMapMarkers();
        }

        private unsafe void OpenRpMap(List<Position> positions)
        {
            IsPlayerInHousingDistract();
            var agent = AgentMap.Instance();
            DalamudContainer.PluginLog.Debug($"Agent Map Id: {agent->SelectedMapId}");
            // TODO Do we need to be careful about clearing map markers? Can we remove specifically the ones we've added?
            agent->ResetMapMarkers();

            // for each entry, mark on map
            positions.ForEach(position =>
            {
                DalamudContainer.PluginLog.Debug($"Adding position: {position.X} {position.Z}");
                var pos = new FFXIVClientStructs.FFXIV.Common.Math.Vector3(position.X, 0, position.Z);
                // Adding extra arguments here to set textPosition to 0. This is to avoid AddMapMarker
                // incorrectly multiplying the position by the CurrentMapSizeFactorFloat and causing issues
                // when viewing other maps
                DalamudContainer.PluginLog.Debug($"{agent->AddMapMarker(pos, 61545, 0, null, 0)}");
            });
        }

        private void CheckAndSubmitPlayerPosition(Object source, ElapsedEventArgs e)
        {
            var player = DalamudContainer.ClientState.LocalPlayer;

            // Player needs to have moved and needs to be roleplaying
            if (HasPlayerMoved(player.Position) && IsPlayerRoleplaying(player.OnlineStatus.GameData.Name))
            {
                // If we're in a housing district, we want to check if we were previously reported as being in a housing district
                // If we weren't, then we want to remove the location data since we're no longer reporting in this zone. If we were
                // already reported as being in a housing district, we don't need to do anything
                if (IsPlayerInHousingDistract())
                {
                    if (!lastReportedInHousing)
                    {
                        lastReportedInHousing = true;
                        connectionService.InvokeHubMethodAsync("RemoveLocationData");
                    }
                } else
                {
                    lastReportedInHousing = false;
                    SendLocationToServer(player);
                }

                
            }
        }

        private unsafe bool IsPlayerInHousingDistract()
        {
            var agent = HousingManager.Instance();
            var currentWard = agent->GetCurrentWard();
            // Returns -1 if we're not in a housing district
            return (currentWard > 0);
        }

        private bool IsPlayerRoleplaying(String status)
        {
            // TODO Turn this into an actual hook to check on OnlineStatus update instead
            // This'll do for now because my two brain cells are tired
            var isRoleplaying = status == "Role-playing";

            if (isRoleplaying != WasRoleplaying)
            {
                WasRoleplaying = isRoleplaying;
                // If we WERE roleplaying and now we're not, we just want to clean up our position from the cache
                if (!isRoleplaying)
                {
                    connectionService.InvokeHubMethodAsync("RemoveLocationData");
                }
            }
            return isRoleplaying;
        }

        private bool HasPlayerMoved(Vector3 playerPos)
        {
            if (Vector3.Distance(lastReportedPosition, playerPos) > DistanceThreshold)
            {
                return true;
            }
            return false;
        }

        private async Task SendLocationToServer(PlayerCharacter player)
        {
            DalamudContainer.PluginLog.Debug("Sending location to server");
            var map = GetCurrentMap();
            DalamudContainer.PluginLog.Debug($"Sending location to server {map.Id.RawString}");
            await connectionService.InvokeHubMethodAsync("SendLocation",
                player.CurrentWorld.Id,
                map.Id.RawString,
                player.Position.X + map.OffsetX,
                player.Position.Z + map.OffsetY);
        }

        private unsafe Map GetCurrentMap()
        {
            var agent = AgentMap.Instance();
            return Maps.GetRow(agent->CurrentMapId);
        }

        private unsafe Map GetSelectedMap()
        {
            var agent = AgentMap.Instance();
            return Maps.GetRow(agent->SelectedMapId);
        }

        public void OnShowDtrChangedHandler(object sender, EventArgs e)
        {
            dtrBarEntry.Shown = this.configuration.ShowSonarDtr;
        }

        private void UpdateDtr()
        {
            SetDtrText();
            dtrBarEntry.OnClick = () => { this.configuration.SonarEnabled = !this.configuration.SonarEnabled; SetDtrText(); };
            dtrBarEntry.Tooltip = "Click to toggle RP Sonar";
        }

        private void SetDtrText()
        {
            var isSonarActive = this.configuration.SonarEnabled && this.configuration.UtilsEnabled && this.connectionService.Connected;
            dtrBarEntry.Text = $"RP: {(isSonarActive ? "On" : "Off")}";
        }


        public void Dispose()
        {
            this.configuration.OnSonarEnabledChanged -= OnConfigChangedHandler;
            this.configuration.OnUtilsEnabledChanged -= OnConfigChangedHandler;
            this.connectionService.OnConnectionChange -= OnConfigChangedHandler;
            this.configuration.OnShowSonarDtrChanged -= OnShowDtrChangedHandler;
            positionCheckTimer.Dispose();
        }
    }
}
