using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using RpUtils.Models;
using RpUtils.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RpUtils.UI.Tabs
{
    public class CurrentRpTab
    {
        // Column Indexes for Roleplayer Count Tabs
        const short SortLocationColumnId = 0;
        const short SortCountColumnId = 1;

        private ConnectionService connectionService;

        // Player count tree nodes.
        private IEnumerable<PlayerCountNode> playerCountNodes = new List<PlayerCountNode>();
        private bool firstSort = true;
        private DateTime? lastUpdated;

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

                if (this.lastUpdated != null)
                {
                    int minutes = (DateTime.Now - this.lastUpdated.Value).Minutes;
                    ImGui.SameLine();
                    if (minutes > 1)
                    {
                        ImGui.Text($" Last updated {minutes} minutes ago.");
                    }
                    else if (minutes == 1)
                    {
                        ImGui.Text($" Last updated {minutes} minute ago.");                    }
                    else
                    {
                        ImGui.Text($" Last updated less than one minute ago.");
                    }
                }

                ImGui.Text($"Currently watching for RP: {currentWatchingForRpCount}");
                ImGui.Spacing();
                
                if (ImGui.BeginTable("roleplayerTableString", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.Sortable))
                {
                    ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.NoReorder | ImGuiTableColumnFlags.DefaultSort);
                    ImGui.TableSetupColumn("Current Roleplayers", ImGuiTableColumnFlags.NoReorder);
                    ImGuiTableSortSpecsPtr? specs = ImGui.TableGetSortSpecs();

                    // First sort is here since the table remembers it's last sort options but is not 'dirty' on load so we want to force the sorting on first rendering.
                    if (firstSort || (specs != null && specs.Value.SpecsDirty && specs.Value.SpecsCount != default))
                    {
                        this.playerCountNodes = this.OrderPlayerCountNodes(this.playerCountNodes, specs);
                        specs.Value.SpecsDirty = false;
                        firstSort = false;
                    }

                    ImGui.TableHeadersRow();

                    // TODO: Extract into a "Render Node" function when we do more than one layer of branch nodes.
                    foreach (var node in this.playerCountNodes)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        bool open = ImGui.TreeNodeEx(node.Location, ImGuiTreeNodeFlags.DefaultOpen);
                        ImGui.TableNextColumn();
                        ImGui.Text(node.Count.ToString());

                        if (open && node.SubLocations.Count() != default)
                        {
                            foreach (var subNode in node.SubLocations)
                            {
                                ImGui.TableNextRow();
                                ImGui.TableNextColumn();
                                ImGui.TreeNodeEx(subNode.Location, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen);
                                ImGui.TableNextColumn();
                                ImGui.Text(subNode.Count.ToString());
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

                    newCounts.Add(new WorldPlayerCount()
                    {
                        WorldName = translatedWorld,
                        Location = translatedMap,
                        Count = entry.Value,
                    });
                }
            }

            var nodes = this.GetPlayerCountNodes(newCounts);
            this.playerCountNodes = this.OrderPlayerCountNodes(nodes, null);
            this.lastUpdated = DateTime.Now;
        }

        /// <summary>
        /// Split the world player counts into nodes for rendering
        /// </summary>
        private IEnumerable<PlayerCountNode> GetPlayerCountNodes(List<WorldPlayerCount> worldPlayerCounts)
        {
            var countTreeNodes = worldPlayerCounts
                .GroupBy(nc => nc.WorldName)
                .Select(x => new PlayerCountNode()
                {
                    Location = x.Key,
                    Count = x.Sum(s => s.Count),
                    SubLocations = x.Select(worldCount => new PlayerCountNode()
                    {
                        Location = worldCount.Location,
                        Count = worldCount.Count,
                    }).ToList()
                })
                .ToList();

            return countTreeNodes;
        }

        /// <summary>
        /// Order world player counts first by world information, then internally by zone.
        /// </summary>
        private IEnumerable<PlayerCountNode> OrderPlayerCountNodes(IEnumerable<PlayerCountNode> source, ImGuiTableSortSpecsPtr? sortSpecs)
        {
            // There is probably a far more elegant way to do this.  We'll figure that out when we get more thigns to sort.
            IEnumerable<PlayerCountNode> nodes;
            bool byLocation = true;
            bool descending = true;

            if (sortSpecs != null && sortSpecs.Value.SpecsCount > 0 && sortSpecs.Value.Specs.SortDirection == ImGuiSortDirection.Descending)
            {
                descending = false;
            }

            if (sortSpecs != null && sortSpecs.Value.SpecsCount > 0 && sortSpecs.Value.Specs.ColumnIndex == SortCountColumnId)
            {
                byLocation = false;
            }

            if (byLocation)
            {
                if (descending)
                {
                    nodes = source.OrderByDescending(x => x.Location);
                    foreach (var node in nodes)
                    {
                        node.SubLocations = node.SubLocations.OrderByDescending(x => x.Location);
                    }
                }
                else
                {
                    nodes = source.OrderBy(x => x.Location);
                    foreach (var node in nodes)
                    {
                        node.SubLocations = node.SubLocations.OrderBy(x => x.Location);
                    }
                }
            }
            else
            {
                if (descending)
                {
                    nodes = source.OrderByDescending(x => x.Count);
                    foreach (var node in nodes)
                    {
                        node.SubLocations = node.SubLocations.OrderByDescending(x => x.Count);
                    }
                }
                else
                {
                    nodes = source.OrderBy(x => x.Count);
                    foreach (var node in nodes)
                    {
                        node.SubLocations = node.SubLocations.OrderBy(x => x.Count);
                    }
                }
            }

            return nodes.ToList();
        }

        public async Task FetchCurrentWatchingForRpCount()
        {
            DalamudContainer.PluginLog.Debug("Fetching currently watching for RP count");
            var count = await connectionService.InvokeHubMethodAsync<int>("GetCurrentWatchingForRpCount");
            currentWatchingForRpCount = count;
        }
    }
}
