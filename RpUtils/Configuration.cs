using Dalamud.Configuration;
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
                    sonarEnabled = value;
                    OnSonarEnabledChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface.SavePluginConfig(this);
        }
    }
}
