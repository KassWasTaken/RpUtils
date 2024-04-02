using ImGuiNET;
using System;
using System.Numerics;

namespace RpUtils
{
    
    // It is good to have this be disposable in general, in case you ever need it to do any cleanup
    class RpUtilsUI : IDisposable
    {
        private Configuration configuration;
        
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
        
        public RpUtilsUI(Configuration configuration)
        {
            this.configuration = configuration;
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

            ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin("RP Utils Configuration", ref this.settingsVisible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {

                var utilsEnabled = this.configuration.UtilsEnabled;
                if (ImGui.Checkbox("Utils Enabled", ref utilsEnabled))
                {
                    this.configuration.UtilsEnabled = utilsEnabled;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    this.configuration.Save();
                }

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
