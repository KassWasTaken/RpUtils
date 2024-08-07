﻿using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace RpUtils
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        [field: NonSerialized] public event EventHandler OnUtilsEnabledChanged;
        private bool utilsEnabled = false;
        public bool UtilsEnabled
        {
            get => utilsEnabled;
            set
            {
                if (utilsEnabled != value)
                {
                    DalamudContainer.PluginLog.Debug("UtilsEnabled Changed");
                    utilsEnabled = value;
                    OnUtilsEnabledChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [field: NonSerialized] public event EventHandler OnSonarEnabledChanged;
        private bool sonarEnabled = false;
        public bool SonarEnabled
        {
            get => sonarEnabled;
            set
            {
                if (sonarEnabled != value)
                {
                    DalamudContainer.PluginLog.Debug("SonarEnabled Changed");
                    sonarEnabled = value;
                    OnSonarEnabledChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        [field: NonSerialized] public event EventHandler OnShowSonarDtrChanged;
        private bool showSonarDtr = true;
        public bool ShowSonarDtr
        {
            get => showSonarDtr;
            set
            {
                if (showSonarDtr != value)
                {
                    DalamudContainer.PluginLog.Debug("ShowSonarDtr Changed");
                    showSonarDtr = value;
                    OnShowSonarDtrChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private IDalamudPluginInterface pluginInterface;

        [NonSerialized]
        public string ApiVersion = "0.1.0";
        [NonSerialized]
        public string ServerAddress = "http://rputils.catwitch.dev:8080";
        [NonSerialized]
        public string HubAddress = "/rpUtilsHub";

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface.SavePluginConfig(this);
        }
    }
}
