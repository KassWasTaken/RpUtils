using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using RpUtils.Models;
using RpUtils.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpUtils.UI.Tabs
{
    public class CurrentRpTab
    {
        private ConnectionService connectionService;
        private Dictionary<string, int> worldMapCounts = new Dictionary<string, int>();
        private List<WorldPlayerCount> playerCountItems = new List<WorldPlayerCount>();
        private IDictionary<string, int> playerCountWorlds = new Dictionary<string, int>();

        private int currentWatchingForRpCount = 0;
        private ExcelSheet<Map> Maps { get; set; }
        private ExcelSheet<World> Worlds { get; set; }

        public CurrentRpTab(ConnectionService connectionService)
        {
            this.connectionService = connectionService;
            Maps = DalamudContainer.DataManager.GetExcelSheet<Map>()!;
            Worlds = DalamudContainer.DataManager.GetExcelSheet<World>()!;
        }

        public void Draw()
        {
            // Draw the current RP tab
            if (ImGui.BeginTabItem("RP Now"))
            {
                // Add a button to fetch world map counts
                if (ImGui.Button("Refresh Counts"))
                {
                    // Since we're in a UI method, we need to avoid blocking. Consider fetching asynchronously.
                    Task.Run(async () =>
                    {
                        await FetchWorldMapCounts();
                        await FetchCurrentWatchingForRpCount();
                    }).ConfigureAwait(false);
                }

                ImGui.Text($"Currently watching for RP: {currentWatchingForRpCount}");
                ImGui.Spacing();

                if (ImGui.BeginTable("roleplayerTableString", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.Sortable))
                {
                    ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.NoReorder);
                    ImGui.TableSetupColumn("Current Roleplayers", ImGuiTableColumnFlags.NoReorder);
                    ImGui.TableHeadersRow();

                    foreach (KeyValuePair<string, int> worldCountItem in this.playerCountWorlds)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        bool open = ImGui.TreeNodeEx(worldCountItem.Key, ImGuiTreeNodeFlags.DefaultOpen);
                        ImGui.TableNextColumn();
                        ImGui.Text(worldCountItem.Value.ToString());

                        if (open)
                        {
                            var currentNodes = this.playerCountItems.Where(x => x.WorldName == worldCountItem.Key).OrderBy(x => x.Location);
                            foreach (var currentNode in currentNodes)
                            {
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TreeNodeEx(currentNode.Location, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen);
                                ImGui.TableNextColumn();
                                ImGui.Text(currentNode.Count.ToString());
                            }
                            ImGui.TreePop();
                        }
                    }
                    ImGui.EndTable();
                }
            }
        }

        public async Task FetchWorldMapCounts()
        {
            DalamudContainer.PluginLog.Debug("Fetching world map counts");
            var rawWorldMapCounts = await connectionService.InvokeHubMethodAsync<Dictionary<string, int>>("GetWorldMapCounts");
            var translatedWorldMapCounts = new Dictionary<string, int>();
            List<WorldPlayerCount> newCounts = new List<WorldPlayerCount>();

            foreach (var entry in rawWorldMapCounts)
            {
                string[] parts = entry.Key.Split(':');
                if (parts.Length == 3) // Ensure the key is in the expected format
                {
                    uint rawWorld = uint.Parse(parts[1]);
                    string rawMap = parts[2];

                    string translatedWorld = Worlds.GetRow(rawWorld).Name.ToString();
                    string translatedMap = Maps.Where(map => map.Id == rawMap).FirstOrDefault()?.PlaceName.Value.Name.ToString() ?? rawMap;
                    translatedWorldMapCounts[$"{translatedWorld} - {translatedMap}"] = entry.Value;

                    newCounts.Add(new WorldPlayerCount()
                    {
                        WorldName = translatedWorld,
                        Location = translatedMap,
                        Count = entry.Value,
                    });
                }
            }

            worldMapCounts = translatedWorldMapCounts;
            playerCountItems = newCounts;
            playerCountWorlds = newCounts
                .GroupBy(nc => nc.WorldName)
                .Select(x => new KeyValuePair<string, int>(x.Key, x.Sum(s => s.Count)))
                .ToDictionary();
        }

        public async Task FetchCurrentWatchingForRpCount()
        {
            DalamudContainer.PluginLog.Debug("Fetching currently watching for RP count");
            var count = await connectionService.InvokeHubMethodAsync<int>("GetCurrentWatchingForRpCount");
            currentWatchingForRpCount = count;
        }
    }
}
