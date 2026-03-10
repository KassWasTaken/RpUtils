using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using RpUtils.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RpUtils.Services;

public sealed class HubConnectionService : IAsyncDisposable, IConnectionStatus
{
    private HubConnection? _connection;
    private readonly Configuration _configuration;
    private readonly CancellationTokenSource _cts = new();

    public event Action<HubConnection>? OnConnected;
    public event Action? OnDisconnected;
    public event Action<Exception?>? OnReconnecting;

    public ConnectionState Status { get; private set; } = ConnectionState.Disconnected;
    public event Action<ConnectionState>? OnStatusChanged;

    public HubConnectionService(Configuration configuration)
    {
        _configuration = configuration;
    }

    public HubConnection? Connection => _connection;
    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public async Task ConnectAsync()
    {
        // If user has disabled RpUtils in the config, we don't connect
        if (!_configuration.EnableRpUtils)
        {
            Plugin.Log.Info("RpUtils is disabled in configuration, skipping connection.");
            SetStatus(ConnectionState.Disabled);
            return;
        }

        // Avoid double connections
        if (_connection is not null)
        {
            Plugin.Log.Debug("Already connected to RpUtils server, skipping connection.");
            return;
        }

        SetStatus(ConnectionState.Connecting);

        var connectionUrl = $"{PluginConstants.ServerAddress}{PluginConstants.HubAddress}?version={PluginConstants.ApiVersion}";
        Plugin.Log.Info($"Connecting to RpUtils server: {connectionUrl}");

        _connection = new HubConnectionBuilder()
            .WithUrl(connectionUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.Closed += ex =>
        {
            if (ex is not null)
                Plugin.Log.Error(ex, "RpUtils connection closed with error.");
            else
                Plugin.Log.Info("RpUtils connection closed.");
            SetStatus(ConnectionState.Disconnected);

            OnDisconnected?.Invoke();
            return Task.CompletedTask;
        };

        _connection.Reconnecting += ex =>
        {
            Plugin.Log.Warning(ex, "RpUtils connection lost, attempting to reconnect...");
            SetStatus(ConnectionState.Reconnecting);
            OnReconnecting?.Invoke(ex);
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
        {
            Plugin.Log.Info("Reconnected to RpUtils server.");
            SetStatus(ConnectionState.Connected);
            OnConnected?.Invoke(_connection);
            return Task.CompletedTask;
        };

        _connection.On<string>("UpdateClient", message =>
        {
            Plugin.Log.Warning($"RpUtils requires update: {message}");

            Plugin.NotificationManager.AddNotification(new Notification
            {
                Content = $"Please update RpUtils: {message}",
                Type = NotificationType.Error,
            });

            // Disconnect — we can't operate with a mismatched version
            Task.Run(async () => await DisconnectAsync());
        });

        OnConnected?.Invoke(_connection);

        try
        {
            await _connection.StartAsync(_cts.Token);
            Plugin.Log.Info("Connected to RpUtils server.");
            SetStatus(ConnectionState.Connected);
        }
        catch (OperationCanceledException)
        {
            Plugin.Log.Warning("Connection attempt was cancelled.");
            SetStatus(ConnectionState.Disconnected);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to connect to RpUtils server.");
            SetStatus(ConnectionState.Disconnected);
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connection is null) return;

        try
        {
            await _connection.StopAsync(CancellationToken.None);
            Plugin.Log.Info("Disconnected from RpUtils server.");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to disconnect from RpUtils server.");
        }
        finally
        {
            await _connection.DisposeAsync();
            _connection = null;
            SetStatus(_configuration.EnableRpUtils ? ConnectionState.Disconnected : ConnectionState.Disabled);
        }
    }

    private void SetStatus(ConnectionState status)
    {
        if (Status == status) return;
        Status = status;
        Plugin.Log.Info($"RpUtils connection status changed: {status}");
        OnStatusChanged?.Invoke(status);
    }

    public async ValueTask DisposeAsync()
    {
        Plugin.Log.Debug("Disposing RpUtils HubConnectionService...");
        _cts.Cancel();

        if (_connection is not null)
        {
            try
            {
                await _connection.DisposeAsync();
            }
            catch (Exception ex)
            {
                Plugin.Log.Error(ex, "Error disposing RpUtils connection.");
            }
        }

        _cts.Dispose();
        Plugin.Log.Debug("RpUtils HubConnectionService disposed.");
    }
}
