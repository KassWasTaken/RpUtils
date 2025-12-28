using Dalamud.Game.ClientState.Objects.SubKinds;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using RpUtils.Models;
using System.Collections.Generic;
using System;
using System.Numerics;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using RpUtils.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using System.Linq;

namespace RpUtils.Controllers
{
    public class SonarController : IDisposable
    {
        private Configuration configuration;
        private ConnectionService connectionService;
        private ExcelSheet<Map> Maps { get; set; }
        private ExcelSheet<TerritoryType> TerritoryTypes { get; set; }
        private ExcelSheet<OnlineStatus> OnlineStatuses { get; set; }

        private DateTime lastPositionCheck = DateTime.MinValue;
        private Vector3 lastReportedPosition = Vector3.Zero;
        private bool lastReportedInHousing = false;
        private const float DistanceThreshold = 5.0f;
        private const int PositionCheckInterval = 10000;
        private bool WasRoleplaying = false;
        private bool previouslyNotifiedSharingLocation = false;
        private bool amIBrodcastingLocation = false;

        // 0 = cities, 1 = overworld, 7 = map-ish zones?, 13 = housing zone, 23 = goldsaucer, 26/47 = diadem, 41 = eureka, 48 = bozja
        private uint[] allowedTerritoryIntendedUses = [0, 1, 7, 23, 26, 47, 41, 48];

        public SonarController(Configuration configuration, ConnectionService connectionService)
        {
            this.configuration = configuration;
            this.connectionService = connectionService;

            Maps = DalamudContainer.DataManager.GetExcelSheet<Map>()!;
            TerritoryTypes = DalamudContainer.DataManager.GetExcelSheet<TerritoryType>()!;
            OnlineStatuses = DalamudContainer.DataManager.GetExcelSheet<OnlineStatus>()!;

            // Adding our subscriber for when the SonarEnabled configuration changes
            this.configuration.OnSonarEnabledChanged += OnConfigChangedHandler;
            this.configuration.OnUtilsEnabledChanged += OnConfigChangedHandler;
            this.connectionService.OnConnectionChange += OnConfigChangedHandler;
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
            DalamudContainer.Framework.Update += OnFrameworkUpdate;
        }

        private void DisableSonar()
        {
            DalamudContainer.Lifecycle.UnregisterListener(Dalamud.Game.Addon.Lifecycle.AddonEvent.PostRefresh, "AreaMap", OnOpenMap);
            DalamudContainer.Framework.Update -= OnFrameworkUpdate;
            ClearMapMarkers();
        }

        private void OnFrameworkUpdate(Dalamud.Plugin.Services.IFramework framework)
        {
            if ((DateTime.Now - lastPositionCheck).TotalMilliseconds >= PositionCheckInterval)
            {
                lastPositionCheck = DateTime.Now;
                CheckAndSubmitPlayerPosition();
            }
        }

        private void OnOpenMap(AddonEvent type, AddonArgs args)
        {
            FindNearbyRp();
        }

        public async Task FindNearbyRp()
        {
            var player = DalamudContainer.ObjectTable?.LocalPlayer;
            var map = GetSelectedMap();

            try
            {
                if (player == null)
                {
                    DalamudContainer.PluginLog.Warning("Player is null in FindNearbyRp");
                    return;
                }

                DalamudContainer.PluginLog.Debug($"Searching for RP in {player.CurrentWorld.RowId}:{map.Id.ExtractText()}");
                var positions = await connectionService.InvokeHubMethodAsync<List<Position>>("GetPlayersInWorldMap", player.CurrentWorld.RowId, map.Id.ExtractText());

                DalamudContainer.PluginLog.Debug($"Positions found: {positions.Count}");

                OpenRpMap(positions);
            }
            catch (Exception ex)
            {
                DalamudContainer.PluginLog.Error($"Error fetching data from server: {ex}");
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
                DalamudContainer.PluginLog.Debug($"Adding position: {position.X} {position.Z}");
                var pos = new FFXIVClientStructs.FFXIV.Common.Math.Vector3(position.X, 0, position.Z);
                // Adding extra arguments here to set textPosition to 0. This is to avoid AddMapMarker
                // incorrectly multiplying the position by the CurrentMapSizeFactorFloat and causing issues
                // when viewing other maps
                DalamudContainer.PluginLog.Debug($"{agent->AddMapMarker(pos, 61545, 0, null, 0)}");
            });
        }

        private void CheckAndSubmitPlayerPosition()
        {
            try
            {
                IPlayerCharacter? player = DalamudContainer.ObjectTable?.LocalPlayer;
                bool isLoggedIn = DalamudContainer.ClientState.IsLoggedIn;
                bool isPvpNotInWolvesDen = DalamudContainer.ClientState.IsPvPExcludingDen;
                bool isRoleplaying = false;
                bool isInHousingDistrict = this.IsPlayerInHousingDistrict();
                ushort territoryTypeId = DalamudContainer.ClientState.TerritoryType;

                if (territoryTypeId == 0 || player == null)
                {
                    return;
                }

                bool isInAllowedTerritoryIntendedUse = allowedTerritoryIntendedUses.Contains(TerritoryTypes.GetRow(territoryTypeId).TerritoryIntendedUse.Value.RowId);
                isRoleplaying = this.IsPlayerRoleplaying(player.OnlineStatus.Value.Name.ExtractText());

                // Handle fail conditions that can cause is to have to remove location data.
                if (amIBrodcastingLocation)
                {
                    if (player == null || !isLoggedIn || isPvpNotInWolvesDen || !isRoleplaying || isInHousingDistrict || !isInAllowedTerritoryIntendedUse)
                    {
                        this.RemoveLocationFromServer();
                        return;
                    }
                }

                if (player != null)
                {
                    // Player needs to have moved and needs to be roleplaying
                    if (HasPlayerMoved(player.Position) && IsPlayerRoleplaying(player.OnlineStatus.Value.Name.ExtractText()) && isInAllowedTerritoryIntendedUse)
                    {
                        // If we're in a housing district, we want to check if we were previously reported as being in a housing district
                        // If we weren't, then we want to remove the location data since we're no longer reporting in this zone. If we were
                        // already reported as being in a housing district, we don't need to do anything
                        if (isInHousingDistrict)
                        {
                            if (!lastReportedInHousing)
                            {
                                lastReportedInHousing = true;
                                this.RemoveLocationFromServer();
                            }
                        }
                        else
                        {
                            lastReportedInHousing = false;
                            SendLocationToServer(player);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DalamudContainer.PluginLog.Error($"Error in CheckAndSubmitPlayerPosition: {ex}");
            }  
        }

        private void NotifySharingLocation()
        {
            if (previouslyNotifiedSharingLocation) { return; }

            var notification = new Dalamud.Interface.ImGuiNotification.Notification();
            notification.Content = "You are now anonymously sharing your location with RpUtils. If this is a private roleplay scene, please turn off the sonar.";
            DalamudContainer.NotificationManager.AddNotification(notification);
            previouslyNotifiedSharingLocation = true;
        }

        private void NotifyNotSharing()
        {
            var notification = new Dalamud.Interface.ImGuiNotification.Notification();
            notification.Content = "You are no longer sharing your location with RpUtils.";
            DalamudContainer.NotificationManager.AddNotification(notification);
            previouslyNotifiedSharingLocation = false;
        }

        private unsafe bool IsPlayerInHousingDistrict()
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
                    this.RemoveLocationFromServer();
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

        private async Task RemoveLocationFromServer()
        {
            DalamudContainer.PluginLog.Debug("Removing location from server");
            await connectionService.InvokeHubMethodAsync("RemoveLocationData");
            this.amIBrodcastingLocation = false;
            this.previouslyNotifiedSharingLocation = false;
            NotifyNotSharing();
        }


        private async Task SendLocationToServer(IPlayerCharacter player)
        {
            if (!previouslyNotifiedSharingLocation)
            {
                NotifySharingLocation();
            }

            DalamudContainer.PluginLog.Debug("Sending location to server");
            var map = GetCurrentMap();
            DalamudContainer.PluginLog.Debug($"Sending location to server {map.Id.ExtractText()}");
            await connectionService.InvokeHubMethodAsync("SendLocation",
                player.CurrentWorld.Value.RowId,
                map.Id.ExtractText(),
                player.Position.X + map.OffsetX,
                player.Position.Z + map.OffsetY);

            this.amIBrodcastingLocation = true;
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
            DalamudContainer.Framework.Update -= OnFrameworkUpdate;
        }
    }
}
