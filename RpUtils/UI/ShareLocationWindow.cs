using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using RpUtils.Models;
using RpUtils.Services;
using RpUtils.Sonar;
using RpUtils.Sonar.Models;
using System.Threading.Tasks;

namespace RpUtils.UI.Windows;

internal class ShareLocationWindow : Window
{
    private readonly IConnectionStatus _connectionStatus;
    private readonly ISonarController _sonarController;

    public ShareLocationWindow(IConnectionStatus connectionStatus, ISonarController sonarController) : base("Share Roleplay Location")
    {
        Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize;

        IsOpen = false;
        _connectionStatus = connectionStatus;
        _sonarController = sonarController;
    }

    private void DrawActivitySelection()
    {
        var selected = SonarActivity.DisplayName(_sonarController.CurrentActivity);

        if (ImGui.BeginCombo("##RoleplayActivity", selected))
        {
            foreach (var activity in SonarActivity.All)
            {
                var isSelected = activity == _sonarController.CurrentActivity;
                if (ImGui.Selectable(SonarActivity.DisplayName(activity), isSelected))
                {
                    var act = activity;
                    _sonarController.SetActivity(act);
                }
                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }
            ImGui.EndCombo();
        }
    }

    public override void Draw()
    {
        var isSharing = _sonarController.IsSharingLocation;
        var isConnected = _connectionStatus.Status == ConnectionState.Connected;
        ImGui.BeginDisabled(!isConnected);
        if (ImGui.Checkbox("Share Roleplay Location", ref isSharing))
        {
            Task.Run(async () =>
            {
                if (isSharing)
                    await _sonarController.StartSharing();
                else
                    await _sonarController.StopSharing();
            });
        }
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Enabling will anonymously share your current location, indicating you are roleplaying and open to walkups.");
        }

        DrawActivitySelection();
        ImGui.EndDisabled();
    }
}
