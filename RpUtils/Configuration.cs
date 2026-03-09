using Dalamud.Configuration;
using System;

namespace RpUtils;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool EnableRpUtils { get; set; } = true;
    public bool ShareRoleplayLocation { get; set; } = false;
    public bool ShowToolbar { get; set; } = true;

    [NonSerialized]
    public string ApiVersion = "0.1.0";
    [NonSerialized]
    public string ServerAddress = "http://localhost:8080";
    [NonSerialized]
    public string HubAddress = "/rpUtilsHub";

    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}