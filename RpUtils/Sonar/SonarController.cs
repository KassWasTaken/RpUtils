using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using RpUtils.Sonar.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RpUtils.Sonar;

public sealed class SonarController : ISonarController, IDisposable
{
    private readonly SonarService _sonarService;

    private readonly Stopwatch _sendTimer = new();
    private readonly TimeSpan _sendInterval = TimeSpan.FromSeconds(5);

    // 0 = cities, 1 = overworld, 7 = map-ish zones?, 13 = housing zone, 23 = goldsaucer, 26/47 = diadem, 41 = eureka, 48 = bozja
    private readonly uint[] _allowedTerritoryIntendedUses = [0, 1, 7, 23, 26, 47, 41, 48];
    private readonly ExcelSheet<TerritoryType> _territoryTypes;
    private readonly ExcelSheet<Map> _maps;
    private readonly ExcelSheet<World> _worlds;

    private int _lastWorld;
    private string _lastMap;
    private string _lastActivity;
    private float _lastPosX;
    private float _lastPosZ;
    private const float _positionThreshold = 0.1f;
    private bool _isInAllowedTerritory = true;

    private List<WorldMapGroup> _groupedCounts = [];
    private Dictionary<string, int>? _worldMapCounts;
    private int _watchingCount;
    private bool _isFetchingCounts;

    public IReadOnlyList<WorldMapGroup> GroupedCounts => _groupedCounts;
    public int WatchingCount => _watchingCount;
    public bool IsFetchingCounts => _isFetchingCounts;

    // Session state
    public bool IsSharingLocation { get; private set; }
    public string CurrentActivity { get; private set; } = SonarActivity.None;

    public event System.Action? OnStateChanged;

    public SonarController(
        SonarService sonarService)
    {
        _sonarService = sonarService;
        _territoryTypes = Plugin.DataManager.GetExcelSheet<TerritoryType>()!;
        _maps = Plugin.DataManager.GetExcelSheet<Map>()!;
        _worlds = Plugin.DataManager.GetExcelSheet<World>()!;
        _lastWorld = 0;
        _lastMap = "";
        _lastActivity = SonarActivity.None;

        Plugin.Framework.Update += OnFrameworkUpdate;
        Plugin.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "AreaMap", OnMapOpened);

        _sonarService.OnReconnected += OnReconnected;
    }

    private void OnReconnected()
    {
        if (IsSharingLocation)
        {
            _lastWorld = 0;
            _lastMap = string.Empty;
        }
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!IsSharingLocation) return;
        if (!_sendTimer.IsRunning || _sendTimer.Elapsed < _sendInterval) return;
        _sendTimer.Restart();

        var localPlayer = Plugin.ObjectTable.LocalPlayer;
        if (localPlayer is null) return;
        var pos = localPlayer.Position;

        var world = (int)Plugin.PlayerState.CurrentWorld.RowId;
        var currentMap = GetCurrentMap();
        if (currentMap is null) return;
        var currentMapText = currentMap.Value.Id.ToString();
        var territoryType = Plugin.ClientState.TerritoryType;
        if (territoryType == 0) return; // Not in a valid territory
        // Checking if we're in an allowed territory. If they weren't and have entered one
        // we want to make sure we're removing their previous location data
        var isAllowed = _allowedTerritoryIntendedUses.Contains(_territoryTypes.GetRow(territoryType).TerritoryIntendedUse.Value.RowId);
        if (!isAllowed)
        {
            if (_isInAllowedTerritory)
            {
                _isInAllowedTerritory = false;
                Task.Run(async () => await _sonarService.RemoveLocationData());
            }
            return;
        }

        if (!_isInAllowedTerritory) _isInAllowedTerritory = true;

        if (!HasMoved(world, currentMapText, pos.X, pos.Z) && _lastActivity == CurrentActivity) return;

        _lastWorld = world;
        _lastMap = currentMapText;
        _lastActivity = CurrentActivity;
        _lastPosX = pos.X;
        _lastPosZ = pos.Z;

        Task.Run(async () =>
        {
            Plugin.Log.Debug("Sending location update: {World}:{Map}:{Activity} at {X}, {Z}", world, currentMap, CurrentActivity, pos.X, pos.Z);
            await _sonarService.SendLocation(world, currentMapText, pos.X, pos.Z, CurrentActivity);
        });
    }

    public Task StartSharing()
    {
        IsSharingLocation = true;
        _isInAllowedTerritory = true;
        // Reset last known position to force an update on start
        _lastWorld = 0;
        _lastMap = String.Empty;
        _lastActivity = SonarActivity.None;
        _sendTimer.Restart();
        Plugin.Log.Info("Started sharing location with activity: {Activity}", CurrentActivity);
        OnStateChanged?.Invoke();

        // Send immediately rather than waiting for the first interval
        OnFrameworkUpdate(Plugin.Framework);
        return Task.CompletedTask;
    }

    public async Task StopSharing()
    {
        IsSharingLocation = false;
        _sendTimer.Stop();
        Plugin.Log.Info("Stopped sharing location.");
        OnStateChanged?.Invoke();

        await _sonarService.RemoveLocationData();
    }

    public async Task ToggleSharing()
    {
        if (IsSharingLocation)
            await StopSharing();
        else
            await StartSharing();
    }

    public Task SetActivity(string activity)
    {
        CurrentActivity = activity;
        Plugin.Log.Info("Activity set to: {Activity}", activity);
        OnStateChanged?.Invoke();
        return Task.CompletedTask;
    }

    public async Task RefreshWorldMapCounts()
    {
        if (_isFetchingCounts) return;

        _isFetchingCounts = true;
        OnStateChanged?.Invoke();

        try
        {
            var countsTask = _sonarService.GetWorldMapCounts();
            var watchingTask = _sonarService.GetCurrentWatchingForRpCount();

            await Task.WhenAll(countsTask, watchingTask);

            _worldMapCounts = countsTask.Result ?? [];
            _watchingCount = watchingTask.Result ?? 0;
            _groupedCounts = BuildGroupedCounts(_worldMapCounts);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to refresh world map counts.");
        }
        finally
        {
            _isFetchingCounts = false;
            OnStateChanged?.Invoke();
        }
    }

    private static string ValidateActivity(string activity)
    {
        var match = SonarActivity.All.FirstOrDefault(a =>
        string.Equals(a, activity, StringComparison.OrdinalIgnoreCase));
        return match ?? SonarActivity.Other;
    }

    private List<WorldMapGroup> BuildGroupedCounts(Dictionary<string, int> raw)
    {
        // Key format: "worldId:mapId:activity"
        var grouped = new Dictionary<int, Dictionary<string, Dictionary<string, int>>>();

        foreach (var (key, count) in raw)
        {
            var parts = key.Split(':', 3);
            if (parts.Length != 3 || !int.TryParse(parts[0], out var worldId)) continue;

            var mapId = parts[1];
            var activity = ValidateActivity(parts[2]);

            if (!grouped.ContainsKey(worldId))
                grouped[worldId] = [];

            if (!grouped[worldId].ContainsKey(mapId))
                grouped[worldId][mapId] = [];

            grouped[worldId][mapId][activity] = count;
        }

        return [.. grouped
            .Select(world => new WorldMapGroup
            {
                WorldName = ResolveWorldName(world.Key),
                TotalCount = world.Value.Values.SelectMany(m => m.Values).Sum(),
                Maps = [.. world.Value
                    .Select(map => new MapActivityGroup
                    {
                        MapName = ResolveMapName(map.Key),
                        TotalCount = map.Value.Values.Sum(),
                        Activities = [.. map.Value
                            .Select(a => new ActivityCount
                            {
                                Activity = a.Key,
                                Count = a.Value,
                            })
                            .OrderByDescending(a => a.Count)],
                    })
                    .OrderByDescending(m => m.TotalCount)],
            })
            .OrderByDescending(w => w.TotalCount)];
    }

    private string ResolveWorldName(int worldId)
    {
        var world = _worlds.GetRow((uint)worldId);
        var name = world.Name.ToString();
        return string.IsNullOrEmpty(name) ? worldId.ToString() : name;
    }

    private string ResolveMapName(string mapId)
    {
        foreach (var row in _maps)
        {
            if (row.Id.ToString() == mapId)
            {
                var name = row.PlaceName.Value.Name.ToString();
                if (!string.IsNullOrEmpty(name)) return name;
            }
        }
        return mapId;
    }

    private void OnMapOpened(AddonEvent type, AddonArgs args)
    {

        Task.Run(async () =>
        {
            try
            {
                await FetchAndPaintMarkers();
            }
            catch (Exception ex)
            {
                Plugin.Log.Error(ex, "Error fetching map markers.");
            }
        });
    }

    private async Task FetchAndPaintMarkers()
    {
        var world = (int)Plugin.PlayerState.CurrentWorld.RowId;
        var map = GetSelectedMap();
        if (map == null) return;

        var mapId = map.Value.Id.ToString();
        Plugin.Log.Debug("Fetching RP positions for {World}:{Map}", world, mapId);

        var positions = await _sonarService.GetPlayersInWorldMap(world, mapId);
        if (positions is null || positions.Count == 0)
        {
            Plugin.Log.Debug("No positions found for {World}:{Map}", world, mapId);
            return;
        }

        Plugin.Log.Info("Found {Count} players in {World}:{Map}", positions.Count, world, mapId);
        PaintMapMarkers(positions);
    }

    private unsafe Map? GetCurrentMap()
    {
        var agent = AgentMap.Instance();
        if (agent == null) return null;
        return _maps.GetRow(agent->CurrentMapId);
    }

    private unsafe Map? GetSelectedMap()
    {
        var agent = AgentMap.Instance();
        if (agent == null) return null;
        return _maps.GetRow(agent->SelectedMapId);
    }

    private unsafe void PaintMapMarkers(List<Position> positions)
    {
        var agent = AgentMap.Instance();
        if (agent == null) return;

        agent->ResetMapMarkers();

        foreach (var position in positions)
        {
            var pos = new FFXIVClientStructs.FFXIV.Common.Math.Vector3(position.X, 0, position.Z);
            agent->AddMapMarker(pos, 61545, 0, null, 0);
        }

        Plugin.Log.Debug("Painted {Count} markers on map.", positions.Count);
    }

    public void Dispose()
    {
        Plugin.Framework.Update -= OnFrameworkUpdate;
        Plugin.AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "AreaMap", OnMapOpened);
        _sonarService.OnReconnected -= OnReconnected;

        if (IsSharingLocation)
        {
            _sonarService.RemoveLocationData().GetAwaiter().GetResult();
        }
    }

    private bool HasMoved(int world, string map, float posX, float posZ)
    {
        // Always send if world or zone changed
        if (world != _lastWorld || map != _lastMap) return true;

        // Only send if position moved beyond threshold
        var dx = posX - _lastPosX;
        var dz = posZ - _lastPosZ;
        return (dx * dx + dz * dz) > _positionThreshold * _positionThreshold;
    }
}