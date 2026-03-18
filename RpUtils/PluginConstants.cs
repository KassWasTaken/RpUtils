namespace RpUtils;

public static class PluginConstants
{
    public const string ApiVersion = "0.2.0";
    public const string HubAddress = "/rpUtilsHub";

    #if DEBUG
        public const string ServerAddress = "http://localhost:8080";
    #else
        public const string ServerAddress = "http://rputils.catwitch.dev:8080";
    #endif
}
