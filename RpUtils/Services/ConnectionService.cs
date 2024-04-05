using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace RpUtils.Services
{
    /// <summary>
    /// Manages the connection to the RpUtils servers, handling both establishment and maintenance of the connection.
    /// </summary>
    public class ConnectionService : IDisposable
    {
        private HubConnection hubConnection;
        private Configuration configuration = new Configuration();

        /// <summary>
        /// Occurs when the connection state changes.
        /// </summary>
        public event EventHandler OnConnectionChange;
        private bool connected = false;
        public bool updateRequired = false;

        /// <summary>
        /// Gets or sets a value indicating whether the connection to the server is established.
        /// </summary>
        public bool Connected
        {
            get => connected;
            set
            {
                if (connected != value)
                {
                    DalamudContainer.PluginLog.Debug($"Connected changed: {value}");
                    connected = value;
                    OnConnectionChange?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionService"/> class.
        /// </summary>
        /// <param name="configuration">The configuration settings for the connection.</param>
        public ConnectionService(Configuration configuration)
        {
            this.configuration = configuration;
            this.configuration.OnUtilsEnabledChanged += OnUtilsEnabledChangedHandler;

            if (this.configuration.UtilsEnabled) { Connect(); }
        }

        /// <summary>
        /// Handles changes to the utility enabled setting.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An EventArgs that contains no event data.</param>
        private async void OnUtilsEnabledChangedHandler(object sender, EventArgs e)
        {
            if (this.configuration.UtilsEnabled) await Connect();
            else await Disconnect();
        }

        /// <summary>
        /// Asynchronously establishes a connection to the RpUtils servers.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task Connect()
        {
            if (!this.configuration.UtilsEnabled || this.Connected || this.updateRequired) {  return; }
            DalamudContainer.PluginLog.Debug("Establishing connection to RpUtils Servers...");

            InitializeHubConnection();
            SubscribeToConnectionEvents();

            try
            {
                await hubConnection.StartAsync();
                this.Connected = true;
            }
            catch (Exception ex)
            {
                DalamudContainer.PluginLog.Debug($"Failed to connect to SignalR server: {ex.Message}");
                this.Connected = false;
            }
        }

        /// <summary>
        /// Initializes the HubConnection and configures logging.
        /// </summary>
        private void InitializeHubConnection()
        {
            DalamudContainer.PluginLog.Debug($"VERSION: {this.configuration.ApiVersion}");
            var connectionUrl = this.configuration.ServerAddress + this.configuration.HubAddress + "?version=" + this.configuration.ApiVersion;
            hubConnection = new HubConnectionBuilder()
                .WithUrl(connectionUrl)
                .WithAutomaticReconnect()
                .ConfigureLogging(logging => {
                    logging.SetMinimumLevel(LogLevel.Debug);
                    logging.AddConsole();
                })
                .Build();
        }

        /// <summary>
        /// Subscribes to connection events to handle when the connection is closed or reconnected.
        /// </summary>
        private void SubscribeToConnectionEvents()
        {
            // Subscribe to our connection closed event
            hubConnection.Closed += async (error) =>
            {
                DalamudContainer.PluginLog.Debug("Connection closed.");
                this.Connected = false;
            };

            hubConnection.Reconnecting += (error) =>
            {
                DalamudContainer.PluginLog.Debug("Reconnecting...");
                this.Connected = false;
                return Task.CompletedTask;
            };

            // Subscribe to our reconected event
            hubConnection.Reconnected += (connectionId) =>
            {
                DalamudContainer.PluginLog.Debug("Reconnected");
                this.Connected = true;
                return Task.CompletedTask;
            };

            hubConnection.On<string>("UpdateClient", (message) =>
            {
                DalamudContainer.PluginLog.Debug($"Server message: {message}");
                this.updateRequired = true;
                this.connected = false;
                var updateNotification = new Dalamud.Interface.ImGuiNotification.Notification();
                updateNotification.Content = "Please update RpUtils: " + message;
                DalamudContainer.NotificationManager.AddNotification(updateNotification);
            });
        }

        /// <summary>
        /// Asynchronously disconnects from the RpUtils servers.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation.</returns>
        private async Task Disconnect()
        {
            DalamudContainer.PluginLog.Debug("Disconnecting from RpUtils Servers");
            try
            {
                await hubConnection.StopAsync();
            }
            catch (Exception ex)
            {
                DalamudContainer.PluginLog.Debug($"Failed to disconnect from server: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if the connection is active and ready.
        /// </summary>
        /// <returns>true if the connection is active; otherwise, false.</returns>
        private bool CheckConnectionReady()
        {
            if (hubConnection == null || hubConnection.State != HubConnectionState.Connected)
            {
                DalamudContainer.PluginLog.Debug("Connection is not active.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Invokes a method on the Hub asynchronously.
        /// </summary>
        /// <typeparam name="T">The return type of the hub method.</typeparam>
        /// <param name="methodName">The name of the hub method to invoke.</param>
        /// <param name="args">The arguments to pass to the hub method.</param>
        /// <returns>A task that represents the asynchronous operation, including the return value of the hub method.</returns>
        public async Task<T> InvokeHubMethodAsync<T>(string methodName, params object[] args)
        {
            if (!CheckConnectionReady())
            {
                throw new InvalidOperationException("Cannot invoke hub method. The connection is not active.");
            }

            try
            {
                DalamudContainer.PluginLog.Debug($"Calling {methodName} with arguments: {string.Join(", ", args)}");
                return await hubConnection.InvokeCoreAsync<T>(methodName, args);
            }
            catch (Exception ex)
            {
                DalamudContainer.PluginLog.Debug($"Error calling {methodName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Invokes a method on the Hub asynchronously without expecting a return value.
        /// </summary>
        /// <param name="methodName">The name of the hub method to invoke.</param>
        /// <param name="args">The arguments to pass to the hub method.</param>
        /// <returns>A task that represents the asynchronous operation of invoking the hub method.</returns>
        public async Task InvokeHubMethodAsync(string methodName, params object[] args)
        {
            if (!CheckConnectionReady())
            {
                throw new InvalidOperationException("Cannot invoke hub method. The connection is not active.");
            }

            try
            {
                DalamudContainer.PluginLog.Debug($"Calling {methodName} with arguments: {string.Join(", ", args)}");
                await hubConnection.SendCoreAsync(methodName, args);
            }
            catch (Exception ex)
            {
                DalamudContainer.PluginLog.Debug($"Error calling {methodName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.configuration.OnUtilsEnabledChanged -= OnUtilsEnabledChangedHandler;
            if (hubConnection != null)
            {
                var disposeTask = hubConnection.DisposeAsync();
                disposeTask.GetAwaiter().GetResult();
            }
        }
    }
}
