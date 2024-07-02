using Dalamud.Plugin;
using Dalamud.IoC;
using Dalamud.Plugin.Services;

public class DalamudContainer
{
    [PluginService]
    public static IClientState ClientState { get; private set; } = null;
    
    [PluginService]
    public static ICommandManager CommandManager { get; private set; } = null;

    [PluginService]
    public static IDalamudPluginInterface PluginInterface { get; private set; } = null;
    
    [PluginService]
    public static IDataManager DataManager { get; private set; } = null;
    
    [PluginService]
    public static IAddonLifecycle Lifecycle { get; private set; } = null;
    
    [PluginService]
    public static IPluginLog PluginLog { get; private set; } = null;
    
    [PluginService]
    public static INotificationManager NotificationManager { get; private set; }

    [PluginService]
    public static IDtrBar DtrBar { get; private set; }

    public static void Initialize(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<DalamudContainer>();
    }
}