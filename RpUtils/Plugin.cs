using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using RpUtils.Services;
using RpUtils.Sonar;
using RpUtils.UI;
using System.Threading.Tasks;

namespace RpUtils;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;

    private const string CommandName = "/rputils";

    private readonly Configuration _configuration;
    private readonly HubConnectionService _hub;
    private readonly SonarService _sonarService;
    private readonly SonarController _sonarController;
    private readonly UIManager _ui;

    public Plugin()
    {
        _configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        
        // Services
        _hub = new HubConnectionService(_configuration);
        _sonarService = new SonarService(_hub);
        _sonarController = new SonarController(_sonarService);

        // UI
        _ui = new UIManager(_configuration, _hub, _sonarController);

        // Commands
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle the display of the Rp Utils toolbar."
        });

        PluginInterface.UiBuilder.Draw += _ui.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += _ui.ToggleConfigWindow;
        PluginInterface.UiBuilder.OpenMainUi += _ui.ToggleToolbarWindow;

        Task.Run(async () => await _hub.ConnectAsync());
    }

    public void Dispose()
    {
        PluginInterface.UiBuilder.Draw -= _ui.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= _ui.ToggleConfigWindow;
        PluginInterface.UiBuilder.OpenMainUi -= _ui.ToggleToolbarWindow;

        CommandManager.RemoveHandler(CommandName);

        _ui.Dispose();
        _sonarController.Dispose();
        _hub.DisposeAsync().AsTask().Wait();
    }

    private void OnCommand(string command, string args)
    {
        Log.Debug($"OnCommand {command}: {args}");
        _ui.ToggleToolbarWindow();
    }
}