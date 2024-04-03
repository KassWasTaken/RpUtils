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

namespace RpUtils.Controllers
{
    public class SonarController : IDisposable
    {
        private Configuration configuration;
        private ConnectionService connectionService;
        private ExcelSheet<TerritoryType> TerritoryTypes { get; set; }
        private ExcelSheet<Map> Maps { get; set; }
        private ExcelSheet<OnlineStatus> OnlineStatuses { get; set; }

        private Timer positionCheckTimer;
        private Vector3 lastReportedPosition = Vector3.Zero;
        private const float DistanceThreshold = 5.0f;
        private const int PositionCheckInterval = 10000;
        private bool WasRoleplaying = false;

        
        public SonarController(Configuration configuration, ConnectionService connectionService) 
        {
            this.configuration = configuration;
            this.connectionService = connectionService;

            TerritoryTypes = DalamudContainer.DataManager.GetExcelSheet<TerritoryType>()!;
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

            // Initial kick off of the sonar if enabled
            if (this.configuration.SonarEnabled && this.configuration.UtilsEnabled) { EnableSonar();  }
        }

        // Handler for our config change listener, we're just going to kick off the toggle
        public void OnConfigChangedHandler(object sender, EventArgs e)
        {
            ToggleSonar();
        }

        // Determines whether to toggle sonar on or off. We need the SonarEnabled, UtilsEnabled, and the ConnectionService to have a connection
        public void ToggleSonar()
        {
            if (this.configuration.SonarEnabled && this.configuration.UtilsEnabled && this.connectionService.Connected)
            {
                EnableSonar();
            }
            else
            {
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

            var agent = AgentMap.Instance();
            DalamudContainer.PluginLog.Debug($"Agent Map Id: {agent->SelectedMapId}");
            // TODO Do we need to be careful about clearing map markers? Can we remove specifically the ones we've added?
            agent->ResetMapMarkers();

            // for each entry, mark on map
            positions.ForEach(position =>
            {
                var pos = new FFXIVClientStructs.FFXIV.Common.Math.Vector3(position.X, 0, position.Z);
                agent->AddMapMarker(pos, 61545);
            });
        }

        private void CheckAndSubmitPlayerPosition(Object source, ElapsedEventArgs e)
        {
            var player = DalamudContainer.ClientState.LocalPlayer;

            if (HasPlayerMoved(player.Position) && IsPlayerRoleplaying(player.OnlineStatus.GameData.Name))
            {
                SendLocationToServer(player);
            }
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


        public void Dispose()
        {
            this.configuration.OnSonarEnabledChanged -= OnConfigChangedHandler;
            this.configuration.OnUtilsEnabledChanged -= OnConfigChangedHandler;
            this.connectionService.OnConnectionChange -= OnConfigChangedHandler;
            positionCheckTimer.Dispose();
        }
    }
}
