using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using RpUtils.Services;
using RpUtils.Sonar;
using RpUtils.UI.Windows;
using System;

namespace RpUtils.UI;

public sealed class UIManager : IDisposable
{
    private readonly WindowSystem _windowSystem = new("RpUtils");

    private readonly ConfigWindow _configWindow;
    private readonly ToolbarWindow _toolbarWindow;
    private readonly LobbyWindow _lobbyWindow;
    private readonly ShareLocationWindow _shareLocationWindow;
    private readonly FindRoleplayWindow _findRoleplayWindow;

    public UIManager(Configuration configuration, IConnectionStatus connectionStatus, ISonarController sonarController)
    {
        _configWindow = new ConfigWindow(configuration, connectionStatus);
        _lobbyWindow = new LobbyWindow();
        _shareLocationWindow = new ShareLocationWindow(connectionStatus, sonarController);
        _findRoleplayWindow = new FindRoleplayWindow(sonarController);
        _toolbarWindow = new ToolbarWindow(
            configuration,
            connectionStatus,
            sonarController,
            () => _shareLocationWindow.Toggle(),
            () => _findRoleplayWindow.Toggle(),
            () => _lobbyWindow.Toggle(),
            () => _configWindow.Toggle()
        );

        _windowSystem.AddWindow(_configWindow);
        _windowSystem.AddWindow(_lobbyWindow);
        _windowSystem.AddWindow(_shareLocationWindow);
        _windowSystem.AddWindow(_findRoleplayWindow);
        _windowSystem.AddWindow(_toolbarWindow);
    }

    public void Draw() => _windowSystem.Draw();
    public void ToggleConfigWindow() => _configWindow.Toggle();
    public void ToggleToolbarWindow() => _toolbarWindow.Toggle();

    public void Dispose()
    {
        _windowSystem.RemoveAllWindows();
    }
}