using Dalamud.Plugin.Services;
using Microsoft.AspNetCore.SignalR.Client;
using RpUtils.Services;
using RpUtils.Sonar.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RpUtils.Sonar;

public sealed class SonarService
{
    private readonly HubConnectionService _hub;

    public SonarService(HubConnectionService hub)
    {
        _hub = hub;
    }

    public async Task SendLocation(int world, string map, float posX, float posZ, string activity)
    {
        try
        {
            if (!_hub.IsConnected) return;
            await _hub.Connection!.InvokeAsync("SendLocation", world, map, posX, posZ, activity);
            Plugin.Log.Debug($"Sent Location: {world}:{map} at {posX}, {posZ}");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to send location.");
        }
    }

    public async Task RemoveLocationData()
    {
        try
        {
            if (!_hub.IsConnected) return;
            await _hub.Connection!.InvokeAsync("RemoveLocationData");
            Plugin.Log.Debug("Sent RemoveLocationData.");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to remove location data.");
        }
    }

    public async Task<List<Position>?> GetPlayersInWorldMap(int world, string map)
    {
        try
        {
            if (!_hub.IsConnected) return null;
            var result = await _hub.Connection!.InvokeAsync<List<Position>>("GetPlayersInWorldMap", world, map);
            Plugin.Log.Debug($"Got {result.Count} players in {world}:{map}");
            return result;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to get players in world map.");
            return null;
        }
    }

    public async Task<Dictionary<string, int>?> GetWorldMapCounts()
    {
        try
        {
            if (!_hub.IsConnected) return null;
            var result = await _hub.Connection!.InvokeAsync<Dictionary<string, int>>("GetWorldMapCounts");
            Plugin.Log.Debug($"Got map counts for {result.Count} maps.");
            return result;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to get world map counts.");
            return null;
        }
    }

    public async Task<int?> GetCurrentWatchingForRpCount()
    {
        try
        {
            if (!_hub.IsConnected) return null;
            var result = await _hub.Connection!.InvokeAsync<int>("GetCurrentWatchingForRpCount");
            Plugin.Log.Debug($"Current watching count: {result}");
            return result;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to get watching count.");
            return null;
        }
    }

}
