using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Dalamud.Bindings.ImGui;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using RpUtils.Models;
using RpUtils.Services;

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
        private string currentWorldName => DalamudContainer.ClientState?.LocalPlayer?.CurrentWorld.Value.Name.ExtractText() ?? string.Empty;

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

                    int buttonId = 1;

                    // TODO: Extract into a "Render Node" function when we do more than one layer of branch nodes.
                    foreach (var node in this.playerCountNodes)
                    {
                        int depth = 0;
                        this.BuildNode(node, depth, ref buttonId, node.Location == this.currentWorldName);
                    }
                    ImGui.EndTable();
                }

                ImGui.EndTabItem();
            }
        }

        private void BuildNode(PlayerCountNode node, int depth, ref int buttonId, bool isSelectable)
        {
            const int maxDepth = 5;

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            bool open = ImGui.TreeNodeEx(node.Location, ImGuiTreeNodeFlags.DefaultOpen);
            ImGui.TableNextColumn();
            ImGui.Text(node.Count.ToString());

            if (open)
            {
                if (node.SubLocations.Count() != default)
                {
                    foreach (var subNode in node.SubLocations)
                    {
                        if (subNode.SubLocations.Count() != default && depth < maxDepth)
                        {
                            this.BuildNode(subNode, depth + 1, ref buttonId, isSelectable);
                        }
                        else
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();
                            ImGui.TreeNodeEx(subNode.Location, ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen);
                            if (subNode.MapId != default && isSelectable)
                            {
                                ImGui.SameLine();
                                buttonId++;
                                ImGui.PushID(buttonId);
                                // if (ImGui.SmallButton($"Map ({subNode.MapId})"))
                                if (ImGui.SmallButton("Map"))
                                {
                                    this.OpenMap(subNode.MapId);
                                }
                                ImGui.PopID();
                            }

                            ImGui.TableNextColumn();
                            ImGui.Text(subNode.Count.ToString());
                        }
                    }
                }
                ImGui.TreePop();
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

                    Map? currentMap = Maps.Where(map => map.Id == rawMap).FirstOrDefault();

                    World world = Worlds.GetRow(rawWorld);

                    string translatedWorld = world.Name.ToString();
                    string translatedMap = currentMap?.PlaceName.Value.Name.ToString();
                    string subMap = currentMap?.PlaceNameSub.Value.Name.ToString();
                    uint mapId = (uint)(currentMap?.RowId ?? default);

                    newCounts.Add(new WorldPlayerCount()
                    {
                        WorldName = translatedWorld,
                        Location = translatedMap,
                        Sublocation = subMap,
                        MapId = mapId,
                        Count = entry.Value,
                    });
                }
            }

            this.playerCountNodes = this.GetPlayerCountNodes(newCounts);
            this.lastUpdated = DateTime.Now;
            this.firstSort = true;
        }

        /// <summary>
        /// Split the world player counts into nodes for rendering
        /// </summary>
        private IEnumerable<PlayerCountNode> GetPlayerCountNodes(List<WorldPlayerCount> worldPlayerCounts)
        {
            List<PlayerCountNode> nodes = new List<PlayerCountNode>();
            foreach (var worldPlayerCount in worldPlayerCounts)
            {
                // Search for existing base world node.
                PlayerCountNode? worldNode = nodes.Where(x => x.Location == worldPlayerCount.WorldName).FirstOrDefault();
                if (worldNode == null)
                {
                    worldNode = new PlayerCountNode()
                    {
                       Location = worldPlayerCount.WorldName,
                       MapId = worldPlayerCount.MapId,
                    };
                    nodes.Add(worldNode);
                }

                worldNode.Count += worldPlayerCount.Count;
                PlayerCountNode? locationNode = worldNode.SubLocations.Where(x => x.Location == worldPlayerCount.Location).FirstOrDefault();
                if (locationNode == null)
                {
                    locationNode = new PlayerCountNode()
                    {
                        Location = worldPlayerCount.Location,
                        MapId = worldPlayerCount.MapId,
                    };
                    worldNode.SubLocations.Add(locationNode);
                }
                else
                {
                    // If we have no sublocation, but a location node already exists for this location, add as a sublocation node.
                    if (worldPlayerCount.Sublocation == string.Empty)
                    {
                        locationNode.Count += worldPlayerCount.Count;
                        locationNode.SubLocations.Add(new PlayerCountNode()
                        {
                            Location = worldPlayerCount.Location,
                            Count = worldPlayerCount.Count,
                            MapId = worldPlayerCount.MapId,
                        });
                    }
                }

                locationNode.Count += worldPlayerCount.Count;

                if (worldPlayerCount.Sublocation != string.Empty)
                {
                    // If we have a sublocation node, but the location already exists, add it as a location node.
                    if (locationNode.SubLocations.Count == 0 && locationNode.SubLocations.Count > 0)
                    {
                        locationNode.SubLocations.Add(new PlayerCountNode()
                        {
                            Location = locationNode.Location,
                            Count = locationNode.Count,
                            MapId = locationNode.MapId,
                        });
                    }

                    // Add the sublocation node.
                    locationNode.SubLocations.Add(new PlayerCountNode()
                    {
                        Location = worldPlayerCount.Sublocation,
                        Count = worldPlayerCount.Count,
                        MapId = worldPlayerCount.MapId,
                    });
                }

            }
            return nodes;
        }


        /// <summary>
        /// Order world player counts first by world information, then internally by zone.
        /// </summary>
        private IList<PlayerCountNode> OrderPlayerCountNodes(IEnumerable<PlayerCountNode> source, ImGuiTableSortSpecsPtr? sortSpecs)
        {
            // There is probably a far more elegant way to do this.  We'll figure that out when we get more thigns to sort.
            IEnumerable<PlayerCountNode> nodes;
            bool byLocation = true;
            bool descending = true;

            if (sortSpecs != null && sortSpecs.Value.SpecsCount > 0 && sortSpecs.Value.Specs.SortDirection == ImGuiSortDirection.Ascending)
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
                        node.SubLocations = this.OrderPlayerCountNodes(node.SubLocations, sortSpecs);
                    }
                }
                else
                {
                    nodes = source.OrderBy(x => x.Location);
                    foreach (var node in nodes)
                    {
                        node.SubLocations = this.OrderPlayerCountNodes(node.SubLocations, sortSpecs);
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
                        node.SubLocations = this.OrderPlayerCountNodes(node.SubLocations, sortSpecs);
                    }
                }
                else
                {
                    nodes = source.OrderBy(x => x.Count);
                    foreach (var node in nodes)
                    {
                        node.SubLocations = this.OrderPlayerCountNodes(node.SubLocations, sortSpecs);
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

        public unsafe void OpenMap(uint mapId)
        {
            var agentMapPtr = AgentMap.Instance();
            agentMapPtr->OpenMapByMapId(mapId);
        }
    }
}
