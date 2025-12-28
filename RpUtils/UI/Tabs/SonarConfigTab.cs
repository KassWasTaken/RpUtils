using Dalamud.Bindings.ImGui;
using RpUtils.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpUtils.UI.Tabs
{
    public class SonarConfigTab
    {
        private bool sonarEnabled;
        private bool showSonarDtr;
        private Configuration configuration;

        public SonarConfigTab(Configuration configuration)
        {
            this.configuration = configuration;
        }

        public void Draw()
        {
            if (ImGui.BeginTabItem("Sonar Config"))
            {
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

                ImGui.PushTextWrapPos(375.0f);
                ImGui.TextUnformatted("This setting will show an entry in the Dalamud info bar, which will show the current status of the sonar, " +
                    "and allow the user to easily open the rputils window.");
                ImGui.PopTextWrapPos();

                var showSonarDtr = this.configuration.ShowSonarDtr;
                if (ImGui.Checkbox("Show Dalamud Info Indicator", ref showSonarDtr))
                {
                    this.configuration.ShowSonarDtr = showSonarDtr;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    this.configuration.Save();
                }
                ImGui.EndTabItem();
            }
        }
    }
}
