using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using RpUtils.Services;
using System.Threading.Tasks;

namespace RpUtils.UI.Windows;

public class ConfigWindow : Window
{
    private readonly Configuration _configuration;
    private readonly IConnectionStatus _connectionStatus;

    public ConfigWindow(Configuration configuration, IConnectionStatus connectionStatus) : base("RpUtils Configuration")
    {
        Flags = ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        _configuration = configuration;
        _connectionStatus = connectionStatus;
    }

    public override void Draw()
    {
        var enableRpUtils = _configuration.EnableRpUtils;
        if (ImGui.Checkbox("Enable RpUtils Connection", ref enableRpUtils))
        {
            
            _configuration.EnableRpUtils = enableRpUtils;
            _configuration.Save();
            Task.Run(async () =>
            {
                if (enableRpUtils)
                {
                    await _connectionStatus.ConnectAsync();
                }
                else
                {
                    await _connectionStatus.DisconnectAsync();
                }

            });
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Toggling off disables the connection to RpUtils server and all features.");
        }
    }
}