using Dalamud.Bindings.ImGui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpUtils.UI.Tabs
{
    public class SettingsTab
    {
        private Configuration configuration;

        public SettingsTab(Configuration configuration)
        {
            this.configuration = configuration;
        }

        public void Draw()
        {
            if (ImGui.BeginTabItem("Settings"))
            {
                ImGui.PushTextWrapPos(375.0f);
                ImGui.TextUnformatted("When enabled, Utils will establish a connection with the RpUtils servers. All other features require" +
                    " Utils to be enabled. When disabled, any connections to the server are broken and all features are suspended.");
                ImGui.PopTextWrapPos();

                var utilsEnabled = this.configuration.UtilsEnabled;
                if (ImGui.Checkbox("Utils Enabled", ref utilsEnabled))
                {
                    this.configuration.UtilsEnabled = utilsEnabled;
                    // can save immediately on change, if you don't want to provide a "Save and Close" button
                    this.configuration.Save();
                }
                ImGui.EndTabItem();
            }
        }
    }
}
