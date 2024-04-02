using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.IoC;
using System.IO;
using System.Reflection;
using System;
using System.Threading.Tasks;
using RpUtils.Controllers;
using RpUtils.Services;

namespace RpUtils
{
    public class RpUtils : IDalamudPlugin
    {
        public string Name => "RP Utils";

        private const string CommandName = "/rputils";

        private Configuration Configuration { get; init; }
        private RpUtilsUI UI { get; init; }
        private SonarController SonarController { get; init; }
        private ConnectionService ConnectionService { get; init; }

        public RpUtils([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
            DalamudContainer.Initialize(pluginInterface);

            this.Configuration = DalamudContainer.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(DalamudContainer.PluginInterface);

            this.ConnectionService = new ConnectionService(this.Configuration);
            this.SonarController = new SonarController(this.Configuration, this.ConnectionService);

            this.UI = new RpUtilsUI(this.Configuration);
            
            DalamudContainer.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the config for RP Utils"
            });

            DalamudContainer.PluginInterface.UiBuilder.Draw += DrawUI;
            DalamudContainer.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            this.UI.Dispose();

            this.ConnectionService.Dispose();
            this.SonarController.Dispose();
            DalamudContainer.CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            this.UI.SettingsVisible = true;
        }

        private void DrawUI()
        {
            this.UI.Draw();
        }

        private void DrawConfigUI()
        {
            this.UI.SettingsVisible = true;
        }
    }
}
