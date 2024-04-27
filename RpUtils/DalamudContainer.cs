using Dalamud.Plugin;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Aetherytes;
using Dalamud.Game.ClientState.Buddy;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Game.ClientState.GamePad;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Gui.PartyFinder;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Libc;
using Dalamud.Game.Network;
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using RpUtils.Services;
using RpUtils.Controllers;
using RpUtils;

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