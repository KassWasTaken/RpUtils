using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using RpUtils.Models;
using RpUtils.Services;
using RpUtils.Sonar;
using System;
using System.Numerics;

namespace RpUtils.UI.Windows
{
    internal class ToolbarWindow : Window
    {
        private readonly IConnectionStatus _connectionStatus;
        private readonly ISonarController _sonar;
        private ISharedImmediateTexture? _rpIcon;

        private readonly Action _toggleShareLocationWindow;
        private readonly Action _toggleFindRoleplayWindow;
        private readonly Action _toggleLobbiesWindow;
        private readonly Action _toggleConfigWindow;

        public ToolbarWindow(
            Configuration configuration, 
            IConnectionStatus connectionStatus, 
            ISonarController sonar, 
            Action toggleShareLocationWindow,
            Action toggleFindRoleplayWindow,
            Action toggleLobbiesWindow,
            Action toggleConfigWindow
            ) : base("##RpUtilsToolbox")
        {
            Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar;

            IsOpen = configuration.ShowToolbar;
            _connectionStatus = connectionStatus;
            _sonar = sonar;
            _rpIcon = Plugin.TextureProvider.GetFromGameIcon(new GameIconLookup(61545));

            _toggleShareLocationWindow = toggleShareLocationWindow;
            _toggleFindRoleplayWindow = toggleFindRoleplayWindow;
            _toggleLobbiesWindow = toggleLobbiesWindow;
            _toggleConfigWindow = toggleConfigWindow;
        }

        private void DrawIconButton(FontAwesomeIcon icon, string tooltip, Action onLeftClick, Action? onRightClick = null)
        {
            ImGuiComponents.IconButton(icon);

            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                onLeftClick.Invoke();
            }

            if (onRightClick is not null && ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                onRightClick.Invoke();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(tooltip);
            }
        }

        private void DrawSonarButton()
        {
            var tooltip = _sonar.IsSharingLocation
                ? $"Sonar\nLeft click: Stop sharing location\nRight click: Open location sharing window"
                : "Sonar\nLeft click: Start sharing location\nRight click: Open location sharing window";
            var onLeftClick = ToggleShareLocation;
            var onRightClick = _toggleShareLocationWindow;
            if (_sonar.IsSharingLocation && _rpIcon != null && _rpIcon.TryGetWrap(out var texture, out _))
            {
                var size = new Vector2(16, 15);
                ImGui.ImageButton(texture.Handle, size);

            } else ImGuiComponents.IconButton(FontAwesomeIcon.MapMarkerAlt);

            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                onLeftClick.Invoke();
            }

            if (onRightClick is not null && ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                onRightClick.Invoke();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(tooltip);
            }
        }

        private void DrawConnectionStatus()
        {

            var status = _connectionStatus.Status;
            var color = status switch
            {
                ConnectionState.Connected => new Vector4(0.0f, 1.0f, 0.0f, 1.0f),
                ConnectionState.Reconnecting => new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                ConnectionState.Connecting => new Vector4(1.0f, 1.0f, 0.0f, 1.0f),
                ConnectionState.Disconnected => new Vector4(1.0f, 0.0f, 0.0f, 1.0f),
                ConnectionState.Disabled => new Vector4(0.5f, 0.5f, 0.5f, 1.0f),
                _ => new Vector4(1.0f, 1.0f, 1.0f, 1.0f),
            };

            ImGui.TextColored(color, $"{status}");
        }

        private void ToggleShareLocation()
        {
            _sonar.ToggleSharing();
        }

        public override bool DrawConditions()
        {
            return Plugin.ClientState.IsLoggedIn;
        }

        public override void Draw()
        {
            var isConnected = _connectionStatus.Status == ConnectionState.Connected;
            using (ImRaii.Disabled(!isConnected))
            {
                ImGui.Text("Rp Utils:");
                ImGui.SameLine();
                DrawConnectionStatus();

                DrawSonarButton();
                ImGui.SameLine();
                DrawIconButton(FontAwesomeIcon.MapMarkedAlt, "Find Roleplay", _toggleFindRoleplayWindow);
                ImGui.SameLine();
                DrawIconButton(FontAwesomeIcon.PeopleGroup, "Lobbies", _toggleLobbiesWindow);
            }
            ImGui.SameLine();
            DrawIconButton(FontAwesomeIcon.Cog, "Settings", _toggleConfigWindow);
        }
    }
}
