using Dalamud.Plugin;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RpUtils.Services
{
 
    public class ConnectionService : IDisposable
    {
        public HubConnection hubConnection;
        private Configuration configuration = new Configuration();

        public ConnectionService(Configuration configuration)
        {
            this.configuration = configuration;
            this.configuration.OnUtilsEnabledChanged += OnUtilsEnabledChangedHandler;
            ToggleConnection();
        }

        public HubConnection getConnection()
        {
            return hubConnection;
        }

        public void OnUtilsEnabledChangedHandler(object sender, EventArgs e)
        {
            ToggleConnection();
        }

        public void ToggleConnection()
        {
            if (this.configuration.UtilsEnabled) Connect();
            else Disconnect();
        }

        public async Task Connect()
        {
            DalamudContainer.PluginLog.Debug("Establishing connection...");
            try
            {
                var connectionUrl = "http://192.168.128.8:8080/rpSonarHub";
                // TODO Actually get configuration details here
                hubConnection = new HubConnectionBuilder()
                    .WithUrl(connectionUrl)
                    .ConfigureLogging(logging => {
                        logging.SetMinimumLevel(LogLevel.Debug);
                        logging.AddConsole();
                    })
                    .Build();

                await hubConnection.StartAsync();
                DalamudContainer.PluginLog.Info($"Connected to {connectionUrl} established: {hubConnection.State}");
            }
            catch (Exception ex)
            {
                DalamudContainer.PluginLog.Error($"Failed to connect to SignalR server: {ex.Message}");
            }
        }

        public async Task Disconnect()
        {
            DalamudContainer.PluginLog.Debug("Disconnecting from RP Sonar Servers");
            try
            {
                await hubConnection.StopAsync();
            }
            catch (Exception ex)
            {
                DalamudContainer.PluginLog.Error($"Failed to disconnect from server: {ex.Message}");
            }
        }

        private bool CheckConnectionReady()
        {
            if (hubConnection == null || hubConnection.State != HubConnectionState.Connected)
            {
                DalamudContainer.PluginLog.Error("Connection is not active.");
                return false;
            }
            return true;
        }

        public async Task<T> InvokeHubMethodAsync<T>(string methodName, params object[] args)
        {
            if (!CheckConnectionReady())
            {
                throw new InvalidOperationException("Cannot invoke hub method. The connection is not active.");
            }

            try
            {
                DalamudContainer.PluginLog.Info($"Calling {methodName} with arguments: {string.Join(", ", args)}");
                return await hubConnection.InvokeCoreAsync<T>(methodName, args);
            }
            catch (Exception ex)
            {
                DalamudContainer.PluginLog.Error($"Error calling {methodName}: {ex.Message}");
                throw;
            }
        }

        public async Task InvokeHubMethodAsync(string methodName, params object[] args)
        {
            if (!CheckConnectionReady())
            {
                throw new InvalidOperationException("Cannot invoke hub method. The connection is not active.");
            }

            try
            {
                DalamudContainer.PluginLog.Info($"Calling {methodName} with arguments: {string.Join(", ", args)}");
                await hubConnection.SendCoreAsync(methodName, args);
            }
            catch (Exception ex)
            {
                DalamudContainer.PluginLog.Error($"Error calling {methodName}: {ex.Message}");
                throw;
            }
        }

        public string GetConnectionStatus()
        {
            if (hubConnection == null) { return "Connection not initialized"; }

            switch (hubConnection.State)
            {
                case HubConnectionState.Connecting:
                    return "Connecting...";
                case HubConnectionState.Connected:
                    return "Connected";
                case HubConnectionState.Reconnecting:
                    return "Reconnecting...";
                case HubConnectionState.Disconnected:
                    return "Disconnected";
                default:
                    return "Unknown";
            }
        }

        public void Dispose()
        {
            this.configuration.OnUtilsEnabledChanged -= OnUtilsEnabledChangedHandler;
            hubConnection.DisposeAsync();
        }
    }
}
