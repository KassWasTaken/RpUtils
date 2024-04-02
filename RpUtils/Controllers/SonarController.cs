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
using Microsoft.AspNetCore.SignalR.Client;
using Dalamud.Plugin;
using RpUtils.Services;
using System.Timers;

namespace RpUtils.Controllers
{
    public class SonarController : IDisposable
    {
        private Configuration configuration;
        private ConnectionService connectionService;
        private ExcelSheet<TerritoryType> TerritoryTypes { get; set; }
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
            OnlineStatuses = DalamudContainer.DataManager.GetExcelSheet<OnlineStatus>()!;
            this.configuration.OnSonarEnabledChanged += OnSonarEnabledChangedHandler;

            positionCheckTimer = new Timer(PositionCheckInterval);
            positionCheckTimer.Elapsed += CheckAndSubmitPlayerPosition;
            positionCheckTimer.AutoReset = true;

            ToggleSonar();
        }

        public void OnSonarEnabledChangedHandler(object sender, EventArgs e)
        {
            ToggleSonar();
        }

        public void ToggleSonar()
        {
            if (this.configuration.SonarEnabled)
            {
                DalamudContainer.Lifecycle.RegisterListener(Dalamud.Game.Addon.Lifecycle.AddonEvent.PostRefresh, "AreaMap", OnOpenMap);
                DalamudContainer.Lifecycle.RegisterListener(Dalamud.Game.Addon.Lifecycle.AddonEvent.PostRefresh, "OnlineStatus", OnOnlineStatusChange);
                positionCheckTimer.Enabled = true;
            }
            else
            {
                DalamudContainer.Lifecycle.UnregisterListener(Dalamud.Game.Addon.Lifecycle.AddonEvent.PostRefresh, "AreaMap", OnOpenMap);
                DalamudContainer.Lifecycle.UnregisterListener(Dalamud.Game.Addon.Lifecycle.AddonEvent.PostRefresh, "OnlineStatus", OnOnlineStatusChange);
                positionCheckTimer.Enabled = false;
            }
        }

        private void OnOnlineStatusChange(AddonEvent type, AddonArgs args)
        {
            DalamudContainer.PluginLog.Debug($"What do we got: Type: {type}, Args: {args}");
        }

        private void OnOpenMap(AddonEvent type, AddonArgs args)
        {
            DalamudContainer.PluginLog.Debug($"What do we got: Type: {type}, Args: {args}");
            FindNearbyRp();
        }

        public async Task FindNearbyRp()
        {
            var player = DalamudContainer.ClientState.LocalPlayer;

            try
            {
                var mapId = TerritoryTypes.GetRow(DalamudContainer.ClientState.TerritoryType).Map.Value.RowId;
                DalamudContainer.PluginLog.Debug($"Searching for RP in {player.CurrentWorld.Id}:{mapId}");
                var positions = await connectionService.InvokeHubMethodAsync<List<Position>>("GetPlayersInWorldMap", player.CurrentWorld.Id, mapId);

                OpenRpMap(positions);
            }
            catch (Exception ex)
            {
                DalamudContainer.PluginLog.Error($"Error fetching data from server: {ex}");
            }
        }

        private unsafe void OpenRpMap(List<Position> positions)
        {

            var agent = AgentMap.Instance();

            DalamudContainer.PluginLog.Debug($"Opening RP Map {agent->ToString()}");
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
            DalamudContainer.PluginLog.Info("Sending location to server");
            var mapId = TerritoryTypes.GetRow(DalamudContainer.ClientState.TerritoryType).Map.Value.RowId;

            await connectionService.InvokeHubMethodAsync("SendLocation",
                player.CurrentWorld.Id,
                mapId,
                player.Position.X,
                player.Position.Z);
        }

        public void Dispose()
        {
            this.configuration.OnSonarEnabledChanged -= OnSonarEnabledChangedHandler;
            positionCheckTimer.Dispose();
        }
    }
}
