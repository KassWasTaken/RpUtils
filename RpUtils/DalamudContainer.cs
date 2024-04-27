using Dalamud.Plugin;
using Dalamud.IoC;
using Dalamud.Plugin.Services;

public class DalamudContainer
{
    [PluginService][RequiredVersion("1.0")] public static IClientState ClientState { get; private set; } = null;
    [PluginService][RequiredVersion("1.0")] public static ICommandManager CommandManager { get; private set; } = null;
    [PluginService][RequiredVersion("1.0")] public static DalamudPluginInterface PluginInterface { get; private set; } = null;
    [PluginService][RequiredVersion("1.0")] public static IDataManager DataManager { get; private set; } = null;
    [PluginService][RequiredVersion("1.0")] public static IAddonLifecycle Lifecycle { get; private set; } = null;
    [PluginService][RequiredVersion("1.0")] public static IPluginLog PluginLog { get; private set; } = null;
    [PluginService][RequiredVersion("1.0")] public static INotificationManager NotificationManager { get; private set; }
    [PluginService][RequiredVersion("1.0")] public static IDtrBar DtrBar { get; private set; }

    public static void Initialize(DalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<DalamudContainer>();
    }
}