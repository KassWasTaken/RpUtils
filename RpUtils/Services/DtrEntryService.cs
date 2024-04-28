using Dalamud.Game.Gui.Dtr;
using RpUtils.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpUtils.Services
{
    public class DtrEntryService : IDisposable
    {
        private Configuration configuration;
        private ConnectionService connectionService;
        private DtrBarEntry dtrBarEntry;
        private MainWindow mainWindow;

        public DtrEntryService(Configuration configuration, ConnectionService connectionService, MainWindow mainWindow)
        {
            this.configuration = configuration;
            this.connectionService = connectionService;
            this.mainWindow = mainWindow;

            // Adding our subscriber for when the SonarEnabled configuration changes
            this.configuration.OnSonarEnabledChanged += OnConfigChangedHandler;
            this.configuration.OnUtilsEnabledChanged += OnConfigChangedHandler;
            this.connectionService.OnConnectionChange += OnConfigChangedHandler;

            // Setting up our DTR bar entry
            dtrBarEntry = DalamudContainer.DtrBar.Get("RP Sonar");
            UpdateDtr();
            this.configuration.OnShowSonarDtrChanged += OnShowDtrChangedHandler;
        }

        // Handler for our config change listener
        public void OnConfigChangedHandler(object sender, EventArgs e)
        {
            SetDtrText();
        }

        public void OnShowDtrChangedHandler(object sender, EventArgs e)
        {
            dtrBarEntry.Shown = this.configuration.ShowSonarDtr;
        }

        private void UpdateDtr()
        {
            SetDtrText();
            dtrBarEntry.OnClick = () => { this.mainWindow.Toggle(); };
            dtrBarEntry.Tooltip = "Click to open RP Utils";
        }

        private void SetDtrText()
        {
            var isSonarActive = this.configuration.SonarEnabled && this.configuration.UtilsEnabled && this.connectionService.Connected;
            dtrBarEntry.Text = $"RP: {(isSonarActive ? "On" : "Off")}";
        }

        public void Dispose()
        {
            this.configuration.OnSonarEnabledChanged -= OnConfigChangedHandler;
            this.configuration.OnUtilsEnabledChanged -= OnConfigChangedHandler;
            this.connectionService.OnConnectionChange -= OnConfigChangedHandler;
            this.configuration.OnShowSonarDtrChanged -= OnShowDtrChangedHandler;
        }
    }
}
