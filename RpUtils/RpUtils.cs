using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.IoC;
using System.IO;
using System.Reflection;
using System;
using System.Threading.Tasks;
using RpUtils.Controllers;
using RpUtils.Services;
using RpUtils.UI;
using Dalamud.Interface.Windowing;

namespace RpUtils
{
    public class RpUtils : IDalamudPlugin
    {
        public string Name => "RP Utils";

        private const string CommandName = "/rputils";

        private Configuration Configuration { get; init; }
        private SonarController SonarController { get; init; }
        private ConnectionService ConnectionService { get; init; }
        private DtrEntryService DtrEntryService { get; init; }

        public readonly WindowSystem WindowSystem = new("RpUtils");
        private MainWindow MainWindow { get; init; }

        public RpUtils([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface)
        {
            DalamudContainer.Initialize(pluginInterface);

            this.Configuration = DalamudContainer.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(DalamudContainer.PluginInterface);

            this.ConnectionService = new ConnectionService(this.Configuration);
            this.SonarController = new SonarController(this.Configuration, this.ConnectionService);

            MainWindow = new MainWindow(this.Configuration, this.ConnectionService);

            WindowSystem.AddWindow(MainWindow);

            this.DtrEntryService = new DtrEntryService(this.Configuration, this.ConnectionService, MainWindow);

            DalamudContainer.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the config for RP Utils"
            });

            DalamudContainer.PluginInterface.UiBuilder.Draw += DrawUI;
            DalamudContainer.PluginInterface.UiBuilder.OpenConfigUi += ToggleMainUI;
            DalamudContainer.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();

            this.MainWindow.Dispose();
            this.ConnectionService.Dispose();
            this.SonarController.Dispose();
            DalamudContainer.CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            ToggleMainUI();
        }

        private void DrawUI() => WindowSystem.Draw();
        public void ToggleConfigUI() => MainWindow.Toggle();
        public void ToggleMainUI() => MainWindow.Toggle();
    }
}
