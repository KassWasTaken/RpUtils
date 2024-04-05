using ImGuiNET;
using RpUtils.Services;
using System;
using System.Numerics;

namespace RpUtils
{
    
    // It is good to have this be disposable in general, in case you ever need it to do any cleanup
    class RpUtilsUI : IDisposable
    {
        private Configuration configuration;
        private ConnectionService connectionService;
        
        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }
        
        public RpUtilsUI(Configuration configuration, ConnectionService connectionService)
        {
            this.configuration = configuration;
            this.connectionService = connectionService;
        }

        public void Dispose()
        {
            
        }

        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.
            DrawMainWindow();
            DrawSettingsWindow();
        }

        public void DrawMainWindow()
        {




        }

        public void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return; 
            }

            ImGui.SetNextWindowSize(new Vector2(300, 300), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin("RP Utils Configuration", ref this.settingsVisible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {

                // Connection status

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


                ImGui.PushTextWrapPos(375.0f);
                ImGui.TextUnformatted("When enabled, Utils will establish a connection with the RpUtils servers. All other features require" +
                    " Utils to be enabled. When disabled, any connections to the server are broken and features are suspended.");
                ImGui.PopTextWrapPos();

                var utilsEnabled = this.configuration.UtilsEnabled;
                if (ImGui.Checkbox("Utils Enabled", ref utilsEnabled))
                {
                    this.configuration.UtilsEnabled = utilsEnabled;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    this.configuration.Save();
                }

                ImGui.PushTextWrapPos(375.0f);
                ImGui.TextUnformatted("Sonar, when enabled, aids in finding open world RP. When the user is set to /roleplaying status," +
                    " the plugin will periodically submit an anonymous position to an RpUtils cache. When opening the map, it will be populated" +
                    "with the anonymous positions in your zone.");
                ImGui.PopTextWrapPos();

                var sonarEnabled = this.configuration.SonarEnabled;
                if (ImGui.Checkbox("Sonar Enabled", ref sonarEnabled))
                {
                    this.configuration.SonarEnabled = sonarEnabled;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    this.configuration.Save();
                }

                //ImGui.Text($"The random config bool is {DalamudContainer.ConnectionService.GetConnectionStatus()}");
            }
            ImGui.End();
        }
    }
}
