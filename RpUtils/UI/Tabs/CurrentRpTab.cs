using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
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

                if (ImGui.BeginTable("Roleplayers Found", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Sortable))
                {
                    // Define the table's column headers
                    ImGui.TableSetupColumn("World - Map");
                    ImGui.TableSetupColumn("Current Roleplayers");
                    ImGui.TableHeadersRow();

                    foreach (var entry in worldMapCounts)
                    {
                        ImGui.TableNextRow();

                        // Column 1: World - Map
                        ImGui.TableNextColumn();
                        ImGui.Text(entry.Key);

                        // Column 2: Count
                        ImGui.TableNextColumn();
                        ImGui.Text(entry.Value.ToString());
                    }

                    // End the table
                    ImGui.EndTable();
                }


                ImGui.EndTabItem();
            }
        }

        public async Task FetchWorldMapCounts()
        {
            DalamudContainer.PluginLog.Debug("Fetching world map counts");
            var rawWorldMapCounts = await connectionService.InvokeHubMethodAsync<Dictionary<string, int>>("GetWorldMapCounts");
            var translatedWorldMapCounts = new Dictionary<string, int>();

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
                }
            }

            worldMapCounts = translatedWorldMapCounts;
        }

        public async Task FetchCurrentWatchingForRpCount()
        {
            DalamudContainer.PluginLog.Debug("Fetching currently watching for RP count");
            var count = await connectionService.InvokeHubMethodAsync<int>("GetCurrentWatchingForRpCount");
            currentWatchingForRpCount = count;
        }
    }
}
