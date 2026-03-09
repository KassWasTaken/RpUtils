using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.Text;

namespace RpUtils.UI.Windows
{
    internal class LobbyWindow : Window
    {
        public LobbyWindow() : base("Lobbies")
        {
            Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize;

            IsOpen = false;
        }

        public override void Draw()
        {
            ImGui.Text("Coming soon...");
        }
    }
}
