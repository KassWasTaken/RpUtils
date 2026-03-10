using Dalamud.Configuration;
using System;

namespace RpUtils;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool EnableRpUtils { get; set; } = true;
    public bool ShowToolbar { get; set; } = true;

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}