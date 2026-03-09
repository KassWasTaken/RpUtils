using System.Collections.Generic;

namespace RpUtils.Sonar.Models;

public class WorldMapGroup
{
    public string WorldName { get; init; } = string.Empty;
    public int TotalCount { get; init; }
    public List<MapActivityGroup> Maps { get; init; } = [];
}

public class MapActivityGroup
{
    public string MapName { get; init; } = string.Empty;
    public int TotalCount { get; init; }
    public List<ActivityCount> Activities { get; init; } = [];
}

public class ActivityCount
{
    public string Activity { get; init; } = string.Empty;
    public int Count { get; init; }
}