using NLog;
using RestEase;
using SyncTrayzor.SyncThing.ApiClient;
using SyncTrayzor.SyncThing.EventWatcher;
using SyncTrayzor.SyncThing.TransferHistory;
using SyncTrayzor.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventWatcher = SyncTrayzor.SyncThing.EventWatcher;

namespace SyncTrayzor.SyncThing
{
    public interface ISyncThingManager : IDisposable
    {
        SyncThingState State { get; }
        bool IsDataLoaded { get; }
        event EventHandler DataLoaded;
        event EventHandler<SyncThingStateChangedEventArgs> StateChanged;
        event EventHandler<MessageLoggedEventArgs> MessageLogged;
        SyncThingConnectionStats TotalConnectionStats { get; }
        event EventHandler<ConnectionStatsChangedEventArgs> TotalConnectionStatsChanged;
        event EventHandler ProcessExitedWithError;
        event EventHandler<DeviceConnectedEventArgs> DeviceConnected;
        event EventHandler<DeviceDisconnectedEventArgs> DeviceDisconnected;

        string ExecutablePath { get; set; }
        string ApiKey { get; set; }
        Uri PreferredAddress { get; set; }
        Uri Address { get; set; }
        IDictionary<string, string> SyncthingEnvironmentalVariables { get; set; }
        string SyncthingCustomHomeDir { get; set; }
        bool SyncthingDenyUpgrade { get; set; }
        bool SyncthingRunLowPriority { get; set; }
        bool SyncthingHideDeviceIds { get; set; }
        TimeSpan SyncthingConnectTimeout { get; set; }
        DateTime StartedTime { get; }
        DateTime LastConnectivityEventTime { get; }
        SyncthingVersion Version { get; }
        ISyncThingFolderManager Folders { get; }
        ISyncThingTransferHistory TransferHistory { get; }

        Task StartAsync();
        Task StopAsync();
        Task StopAndWaitAsync();
        Task RestartAsync();
        void Kill();
        void KillAllSyncthingProcesses();

        bool TryFetchDeviceById(string deviceId, out Device device);
        IReadOnlyCollection<Device> FetchAllDevices();

        Task ScanAsync(string folderId, string subPath);
        Task ReloadIgnoresAsync(string folderId);
    }

    public class SyncThingManager : ISyncThingManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly SynchronizedEventDispatcher eventDispatcher;
        private readonly ISyncThingProcessRunner processRunner;
        private readonly ISyncThingApiClientFactory apiClientFactory;

        // This lock covers the eventWatcher, connectionsWatcher, apiClients, and the CTS
        private readonly object apiClientsLock = new object();
        private readonly ISyncThingEventWatcher eventWatcher;
        private readonly ISyncThingConnectionsWatcher connectionsWatcher;
        private readonly SynchronizedTransientWrapper<ISyncThingApiClient> apiClient;
        private readonly IFreePortFinder freePortFinder;
        private CancellationTokenSource apiAbortCts;

        private DateTime _startedTime;
        private readonly object startedTimeLock = new object();
        public DateTime StartedTime
        {
            get { lock (this.startedTimeLock) { return this._startedTime; } }
            set { lock (this.startedTimeLock) { this._startedTime = value; } }
        }

        private DateTime _lastConnectivityEventTime;
        private readonly object lastConnectivityEventTimeLock = new object();
        public DateTime LastConnectivityEventTime
        {
            get { lock (this.lastConnectivityEventTimeLock) { return this._lastConnectivityEventTime; } }
            private set { lock (this.lastConnectivityEventTimeLock) { this._lastConnectivityEventTime = value; } }
        }

        private readonly object stateLock = new object();
        private SyncThingState _state;
        public SyncThingState State
        {
            get { lock (this.stateLock) { return this._state; } }
            set { lock (this.stateLock) { this._state = value; } }
        }

        public bool IsDataLoaded { get; private set; }
        public event EventHandler DataLoaded;
        public event EventHandler<SyncThingStateChangedEventArgs> StateChanged;
        public event EventHandler<MessageLoggedEventArgs> MessageLogged;
        public event EventHandler<DeviceConnectedEventArgs> DeviceConnected;
        public event EventHandler<DeviceDisconnectedEventArgs> DeviceDisconnected;

        private readonly object totalConnectionStatsLock = new object();
        private SyncThingConnectionStats _totalConnectionStats;
        public SyncThingConnectionStats TotalConnectionStats
        {
            get { lock (this.totalConnectionStatsLock) { return this._totalConnectionStats; } }
            set { lock (this.totalConnectionStatsLock) { this._totalConnectionStats = value; } }
        }
        public event EventHandler<ConnectionStatsChangedEventArgs> TotalConnectionStatsChanged;

        public event EventHandler ProcessExitedWithError;

        public string ExecutablePath { get; set; }
        public string ApiKey { get; set; }
        public Uri PreferredAddress { get; set; }
        public Uri Address { get; set; }
        public string SyncthingCustomHomeDir { get; set; }
        public IDictionary<string, string> SyncthingEnvironmentalVariables { get; set; }
        public bool SyncthingDenyUpgrade { get; set; }
        public bool SyncthingRunLowPriority { get; set; }
        public bool SyncthingHideDeviceIds { get; set; }
        public TimeSpan SyncthingConnectTimeout { get; set; }

        private readonly object devicesLock = new object();
        private ConcurrentDictionary<string, Device> _devices = new ConcurrentDictionary<string, Device>();
        public ConcurrentDictionary<string, Device> devices
        {
            get { lock (this.devicesLock) { return this._devices; } }
            set { lock (this.devicesLock) this._devices = value; }
        }

        public SyncthingVersion Version { get; private set; }

        private readonly SyncThingFolderManager _folders;
        public ISyncThingFolderManager Folders
        {
            get { return this._folders; }
        }

        private readonly ISyncThingTransferHistory _transferHistory;
        public ISyncThingTransferHistory TransferHistory
        {
            get { return this._transferHistory; }
        }

        public SyncThingManager(
            ISyncThingProcessRunner processRunner,
            ISyncThingApiClientFactory apiClientFactory,
            ISyncThingEventWatcherFactory eventWatcherFactory,
            ISyncThingConnectionsWatcherFactory connectionsWatcherFactory,
            IFreePortFinder freePortFinder)
        {
            this.StartedTime = DateTime.MinValue;
            this.LastConnectivityEventTime = DateTime.MinValue;

            this.eventDispatcher = new SynchronizedEventDispatcher(this);
            this.processRunner = processRunner;
            this.apiClientFactory = apiClientFactory;
            this.freePortFinder = freePortFinder;

            this.apiClient = new SynchronizedTransientWrapper<ISyncThingApiClient>(this.apiClientsLock);

            this.eventWatcher = eventWatcherFactory.CreateEventWatcher(this.apiClient);
            this.eventWatcher.DeviceConnected += (o, e) => this.OnDeviceConnected(e);
            this.eventWatcher.DeviceDisconnected += (o, e) => this.OnDeviceDisconnected(e);

            this.connectionsWatcher = connectionsWatcherFactory.CreateConnectionsWatcher(this.apiClient);
            this.connectionsWatcher.TotalConnectionStatsChanged += (o, e) => this.OnTotalConnectionStatsChanged(e.TotalConnectionStats);

            this._folders = new SyncThingFolderManager(this.apiClient, this.eventWatcher, TimeSpan.FromMinutes(10));
            this._transferHistory = new SyncThingTransferHistory(this.eventWatcher, this._folders);

            this.processRunner.ProcessStopped += (o, e) => this.ProcessStopped(e.ExitStatus);
            this.processRunner.MessageLogged += (o, e) => this.OnMessageLogged(e.LogMessage);
            this.processRunner.ProcessRestarted += (o, e) => this.ProcessRestarted();
            this.processRunner.Starting += (o, e) => this.ProcessStarting();
        }

        public async Task StartAsync()
        {
            this.processRunner.Start();
            await this.StartClientAsync();
        }

        public async Task StopAsync()
        {
            if (this.State != SyncThingState.Running)
                return;

            await this.apiClient.Value.ShutdownAsync();
            this.SetState(SyncThingState.Stopping);
        }

        public async Task StopAndWaitAsync()
        {
            var apiClient = this.apiClient.Value;
            if (apiClient != null)
                return Task.FromResult(false);

            var tcs = new TaskCompletionSource<object>();
            EventHandler<SyncThingStateChangedEventArgs> stateChangedHandler = (o, e) =>
            {
                if (e.NewState == SyncThingState.Stopped)
                    tcs.TrySetResult(null);
                else if (e.NewState != SyncThingState.Stopping)
                    tcs.TrySetException(new Exception(String.Format("Failed to stop Syncthing: Went to state {0} instead", e.NewState)));
            };
            this.StateChanged += stateChangedHandler;

            await apiClient.ShutdownAsync();
            this.SetState(SyncThingState.Stopping);

            await tcs.Task;
            this.StateChanged -= stateChangedHandler;
        }

        public Task RestartAsync()
        {
            if (this.State != SyncThingState.Running)
                return Task.FromResult(false);

            return this.apiClient.Value.RestartAsync();
        }

        public void Kill()
        {
            this.processRunner.Kill();
            this.SetState(SyncThingState.Stopped);
        }

        public void KillAllSyncthingProcesses()
        {
            this.processRunner.KillAllSyncthingProcesses();
        }  

        public bool TryFetchDeviceById(string deviceId, out Device device)
        {
            return this.devices.TryGetValue(deviceId, out device);
        }

        public IReadOnlyCollection<Device> FetchAllDevices()
        {
            return new List<Device>(this.devices.Values).AsReadOnly();
        }

        public Task ScanAsync(string folderId, string subPath)
        {
            return this.apiClient.Value.ScanAsync(folderId, subPath);
        }

        public Task ReloadIgnoresAsync(string folderId)
        {
            return this._folders.ReloadIgnoresAsync(folderId);
        }

        private void SetState(SyncThingState state)
        {
            SyncThingState oldState;
            bool abortApi = false;
            lock (this.stateLock)
            {
                logger.Debug("Request to set state: {0} -> {1}", this._state, state);
                if (state == this._state)
                    return;

                oldState = this._state;
                // We really need a proper state machine here....
                // There's a race if Syncthing can't start because the database is locked by another process on the same port
                // In this case, we see the process as having failed, but the event watcher chimes in a split-second later with the 'Started' event.
                // This runs the risk of transitioning us from Stopped -> Starting -> Stopped -> Running, which is bad news for everyone
                // So, get around this by enforcing strict state transitions.
                if (this._state == SyncThingState.Stopped && state == SyncThingState.Running)
                    return;

                if (this._state == SyncThingState.Running ||
                    (this._state == SyncThingState.Starting && state == SyncThingState.Stopped))
                    abortApi = true;

                logger.Debug("Setting state: {0} -> {1}", this._state, state);
                this._state = state;
            }

            if (abortApi)
            {
                logger.Debug("Aborting API clients");
                lock (this.apiClientsLock)
                {
                    this.apiAbortCts.Cancel();
                    this.StopApiClients();
                }
            }

            this.eventDispatcher.Raise(this.StateChanged, new SyncThingStateChangedEventArgs(oldState, state));
        }

        private async Task CreateApiClientAsync(CancellationToken cancellationToken)
        {
            logger.Debug("Starting API clients");
            var apiClient = await this.apiClientFactory.CreateCorrectApiClientAsync(this.Address, this.ApiKey, this.SyncthingConnectTimeout, cancellationToken);
            logger.Debug("Have the API client! It's {0}", apiClient.GetType().Name);

            this.apiClient.Value = apiClient;

            this.SetState(SyncThingState.Running);
        }

        private async Task StartClientAsync()
        {
            try
            {
                this.apiAbortCts = new CancellationTokenSource();
                await this.CreateApiClientAsync(this.apiAbortCts.Token);
                await this.LoadStartupDataAsync(this.apiAbortCts.Token);
                this.StartWatchers(this.apiAbortCts.Token);
            }
            catch (OperationCanceledException) // If Syncthing dies on its own, etc
            {
                logger.Info("StartClientAsync aborted");
            }
            catch (ApiException e)
            {
                var msg = String.Format("Refit Error. StatusCode: {0}. Content: {1}. Reason: {2}", e.StatusCode, e.Content, e.ReasonPhrase);
                logger.Error(msg, e);
                throw new SyncThingDidNotStartCorrectlyException(msg, e);
            }
            catch (HttpRequestException e)
            {
                var msg = String.Format("HttpRequestException while starting Syncthing", e);
                logger.Error(msg, e);
                throw new SyncThingDidNotStartCorrectlyException(msg, e);
            }
            catch (Exception e)
            {
                logger.Error("Error starting Syncthing API", e);
                throw e;
            }
        }

        private void StartWatchers(CancellationToken cancellationToken)
        {
            // This is all synchronous, so it's safe to execute inside the lock
            lock (this.apiClientsLock)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (apiClient == null)
                    throw new InvalidOperationException("ApiClient must not be null");

                this.connectionsWatcher.Start();
                this.eventWatcher.Start();
            }
        }

        private void StopApiClients()
        {
            lock (this.apiClientsLock)
            {
                if (this.apiAbortCts != null)
                    this.apiAbortCts.Cancel();

                this.apiClient.UnsynchronizedValue = null;

                this.connectionsWatcher.Stop();
                this.eventWatcher.Stop();
            }
        }

        private async void ProcessStarting()
        {
            var port = this.freePortFinder.FindFreePort(this.PreferredAddress.Port);
            var uriBuilder = new UriBuilder(this.PreferredAddress);
            uriBuilder.Port = port;
            this.Address = uriBuilder.Uri;

            this.processRunner.ApiKey = this.ApiKey;
            this.processRunner.HostAddress = this.Address.ToString();
            this.processRunner.ExecutablePath = this.ExecutablePath;
            this.processRunner.CustomHomeDir = this.SyncthingCustomHomeDir;
            this.processRunner.EnvironmentalVariables = this.SyncthingEnvironmentalVariables;
            this.processRunner.DenyUpgrade = this.SyncthingDenyUpgrade;
            this.processRunner.RunLowPriority = this.SyncthingRunLowPriority;
            this.processRunner.HideDeviceIds = this.SyncthingHideDeviceIds;

            var isRestart = (this.State == SyncThingState.Restarting);
            this.SetState(SyncThingState.Starting);

            // Catch restart cases, and re-start the API
            // This isn't ideal, as we don't get to nicely propagate any exceptions to the UI
            if (isRestart)
                await this.StartClientAsync();
        }

        private void ProcessStopped(SyncThingExitStatus exitStatus)
        {
            this.SetState(SyncThingState.Stopped);
            if (exitStatus == SyncThingExitStatus.Error)
                this.OnProcessExitedWithError();
        }

        private void ProcessRestarted()
        {
            this.SetState(SyncThingState.Restarting);
        }

        private async Task LoadStartupDataAsync(CancellationToken cancellationToken)
        {
            logger.Debug("Startup Complete! Loading startup data");

            // There's a race where Syncthing died, and so we kill the API clients and set it to null,
            // but we still end up here, because threading.
            ISyncThingApiClient apiClient;
            lock (this.apiClientsLock)
            {
                cancellationToken.ThrowIfCancellationRequested();
                apiClient = this.apiClient.UnsynchronizedValue;
                if (apiClient == null)
                    throw new InvalidOperationException("ApiClient must not be null");
            }

            var configTask = apiClient.FetchConfigAsync();
            var systemTask = apiClient.FetchSystemInfoAsync();
            var versionTask = apiClient.FetchVersionAsync();
            var connectionsTask = apiClient.FetchConnectionsAsync();

            cancellationToken.ThrowIfCancellationRequested();
            await Task.WhenAll(configTask, systemTask, versionTask, connectionsTask);

            // We can potentially see duplicate devices (if the user set their config file that way). Ignore them.
            var devices = configTask.Result.Devices.DistinctBy(x => x.DeviceID).Select(device =>
            {
                var deviceObj = new Device(device.DeviceID, device.Name);
                ItemConnectionData connectionData;
                if (connectionsTask.Result.DeviceConnections.TryGetValue(device.DeviceID, out connectionData))
                    deviceObj.SetConnected(connectionData.Address);
                return deviceObj;
            });
            this.devices = new ConcurrentDictionary<string, Device>(devices.Select(x => new KeyValuePair<string, Device>(x.DeviceId, x)));

            await this._folders.LoadFoldersAsync(configTask.Result, systemTask.Result, cancellationToken);

            this.Version = versionTask.Result;

            cancellationToken.ThrowIfCancellationRequested();
            
            this.StartedTime = DateTime.UtcNow;
            this.IsDataLoaded = true;
            this.OnDataLoaded();
        }

        private void OnDeviceConnected(EventWatcher.DeviceConnectedEventArgs e)
        {
            Device device;
            if (!this.devices.TryGetValue(e.DeviceId, out device))
            {
                logger.Warn("Unexpected device connected: {0}, address {1}. It wasn't fetched when we fetched our config", e.DeviceId, e.Address);
                return; // Not expecting this device! It wasn't in the config...
            }

            device.SetConnected(e.Address);
            this.LastConnectivityEventTime = DateTime.UtcNow;

            this.eventDispatcher.Raise(this.DeviceConnected, new DeviceConnectedEventArgs(device));
        }

        private void OnDeviceDisconnected(EventWatcher.DeviceDisconnectedEventArgs e)
        {
            Device device;
            if (!this.devices.TryGetValue(e.DeviceId, out device))
            {
                logger.Warn("Unexpected device connected: {0}, error {1}. It wasn't fetched when we fetched our config", e.DeviceId, e.Error);
                return; // Not expecting this device! It wasn't in the config...
            }

            device.SetDisconnected();
            this.LastConnectivityEventTime = DateTime.UtcNow;

            this.eventDispatcher.Raise(this.DeviceDisconnected, new DeviceDisconnectedEventArgs(device));
        }

        private void OnMessageLogged(string logMessage)
        {
            this.eventDispatcher.Raise(this.MessageLogged, new MessageLoggedEventArgs(logMessage));
        }

        private void OnTotalConnectionStatsChanged(SyncThingConnectionStats stats)
        {
            this.TotalConnectionStats = stats;
            this.eventDispatcher.Raise(this.TotalConnectionStatsChanged, new ConnectionStatsChangedEventArgs(stats));
        }

        private void OnDataLoaded()
        {
            this.eventDispatcher.Raise(this.DataLoaded);
        }

        private void OnProcessExitedWithError()
        {
            this.eventDispatcher.Raise(this.ProcessExitedWithError);
        }

        public void Dispose()
        {
            this.processRunner.Dispose();
            this.StopApiClients();
            this.eventWatcher.Dispose();
            this.connectionsWatcher.Dispose();
        }
    }
}
