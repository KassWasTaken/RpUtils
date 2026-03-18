using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using RpUtils.Sonar;
using RpUtils.Sonar.Models;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RpUtils.UI.Windows;

internal class FindRoleplayWindow : Window
{
    private readonly ISonarController _sonar;
    private readonly Stopwatch _refreshTimer = new();
    private readonly TimeSpan _refreshInterval = TimeSpan.FromSeconds(15);

    public FindRoleplayWindow(ISonarController sonar) : base("Currently Roleplaying...")
    {
        IsOpen = false;
        _sonar = sonar;
    }

    public override void OnOpen()
    {
        Task.Run(async () => await _sonar.RefreshWorldMapCounts());
        _refreshTimer.Restart();
    }

    public override void OnClose()
    {
        _refreshTimer.Stop();
    }

    public override void Draw()
    {
        if (_refreshTimer.Elapsed >= _refreshInterval)
        {
            _refreshTimer.Restart();
            Task.Run(async () => await _sonar.RefreshWorldMapCounts());
        }

        ImGui.Separator();

        if (_sonar.IsFetchingCounts && _sonar.GroupedCounts.Count == 0)
        {
            ImGui.Text("Loading...");
            return;
        }

        if (_sonar.GroupedCounts.Count == 0)
        {
            ImGui.Text("No active roleplay.");
            return;
        }

        ImGuiTableFlags tableFlags = ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuterH
            | ImGuiTableFlags.Resizable | ImGuiTableFlags.NoBordersInBody
            | ImGuiTableFlags.RowBg;

        using var table = ImRaii.Table("Find Roleplay", 2, tableFlags);
        if (!table) return;

        ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Count", ImGuiTableColumnFlags.WidthFixed, 50);
        ImGui.TableHeadersRow();

        foreach (var world in _sonar.GroupedCounts)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            using var worldNode = ImRaii.TreeNode(world.WorldName, ImGuiTreeNodeFlags.SpanFullWidth);
            ImGui.TableNextColumn();
            ImGui.Text(world.TotalCount.ToString());

            if (!worldNode) continue;

            foreach (var map in world.Maps)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                using var mapNode = ImRaii.TreeNode(map.MapName, ImGuiTreeNodeFlags.SpanFullWidth);
                ImGui.TableNextColumn();
                ImGui.Text(map.TotalCount.ToString());

                if (!mapNode) continue;

                foreach (var activity in map.Activities)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.TreeNodeEx(SonarActivity.DisplayName(activity.Activity),
                        ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiTreeNodeFlags.SpanFullWidth);
                    ImGui.TableNextColumn();
                    ImGui.Text(activity.Count.ToString());
                }
            }
        }
    }
}