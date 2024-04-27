using Dalamud.Interface.Windowing;
using ImGuiNET;
using RpUtils.Services;
using RpUtils.UI.Tabs;
using System;
using System.Numerics;

namespace RpUtils.UI
{
    public class MainWindow : Window, IDisposable
    {
        private Configuration configuration;
        private ConnectionService connectionService;

        private SettingsTab settingsTab;
        private SonarConfigTab sonarConfigTab;
        private CurrentRpTab currentRpTab;

        public MainWindow(Configuration configuration, ConnectionService connectionService) : base("Rp Utils")
        {
            this.configuration = configuration;
            this.connectionService = connectionService;
            this.settingsTab = new SettingsTab(this.configuration);
            this.sonarConfigTab = new SonarConfigTab(this.configuration);
            this.currentRpTab = new CurrentRpTab(this.connectionService);

            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(400, 330),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
        }

        public void Dispose()
        {

        }

        public override void Draw()
        {
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(400, 300), ImGuiCond.FirstUseEver);

            DrawConnectionStatus();

            if (ImGui.BeginTabBar("RpUtilsTabs"))
            {

                this.settingsTab.Draw();
                this.sonarConfigTab.Draw();
                this.currentRpTab.Draw();

                ImGui.EndTabBar();
            }

        }

        private void DrawConnectionStatus()
        {
            var connectionStatus = "";
            if (this.connectionService.Connected)
            {
                connectionStatus = "Connected";
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 1, 0, 1)); // Green for "Connected"
            }
            else if (this.connectionService.updateRequired)
            {
                connectionStatus = "Update Required";
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0, 0, 1)); // Red for "Update Required"
            }
            else
            {
                connectionStatus = "Disconnected";
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 0, 1)); // Yellow for "Disconnected"
            }
            ImGui.Text($"Status: {connectionStatus}");
            ImGui.PopStyleColor();
        }
    }
}
