namespace RpUtils.Sonar.Models;

public static class SonarActivity
{
    public const string None = "None";
    public const string Sparring = "Sparring";
    public const string Adventuring = "Adventuring";
    public const string SliceOfLife = "Slice of Life";
    public const string SocialGathering = "Social Gathering";
    public const string Event = "Event";
    public const string Combat = "Combat";
    public const string Other = "Other";

    public static readonly string[] All =
    {
        None, Sparring, Adventuring, SliceOfLife, SocialGathering, Event, Combat, Other
    };

    public static string DisplayName(string activity)
    {
        return activity == None ? "No Activity" : activity;
    }
}