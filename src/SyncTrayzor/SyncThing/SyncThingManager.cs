using NLog;
using RestEase;
using SyncTrayzor.Services.Config;
using SyncTrayzor.SyncThing.ApiClient;
using SyncTrayzor.SyncThing.EventWatcher;
using SyncTrayzor.SyncThing.TransferHistory;
using SyncTrayzor.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using SyncTrayzor.SyncThing.DebugFacilities;

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
        List<string> SyncthingCommandLineFlags { get; set; }
        IDictionary<string, string> SyncthingEnvironmentalVariables { get; set; }
        string SyncthingCustomHomeDir { get; set; }
        bool SyncthingDenyUpgrade { get; set; }
        SyncThingPriorityLevel SyncthingPriorityLevel { get; set; }
        bool SyncthingHideDeviceIds { get; set; }
        TimeSpan SyncthingConnectTimeout { get; set; }
        DateTime StartedTime { get; }
        DateTime LastConnectivityEventTime { get; }
        SyncThingVersionInformation Version { get; }
        ISyncThingFolderManager Folders { get; }
        ISyncThingTransferHistory TransferHistory { get; }
        ISyncThingDebugFacilitiesManager DebugFacilities { get; }

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

        private SystemInfo systemInfo;

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
        public List<string> SyncthingCommandLineFlags { get; set; } = new List<string>();
        public IDictionary<string, string> SyncthingEnvironmentalVariables { get; set; } = new Dictionary<string, string>();
        public bool SyncthingDenyUpgrade { get; set; }
        public SyncThingPriorityLevel SyncthingPriorityLevel { get; set; }
        public bool SyncthingHideDeviceIds { get; set; }
        public TimeSpan SyncthingConnectTimeout { get; set; }

        private readonly object devicesLock = new object();
        private ConcurrentDictionary<string, Device> _devices = new ConcurrentDictionary<string, Device>();
        public ConcurrentDictionary<string, Device> devices
        {
            get { lock (this.devicesLock) { return this._devices; } }
            set { lock (this.devicesLock) this._devices = value; }
        }

        public SyncThingVersionInformation Version { get; private set; }

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

        private SyncThingDebugFacilitiesManager _debugFacilities;
        public ISyncThingDebugFacilitiesManager DebugFacilities
        {
            get { return this._debugFacilities; }
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
            this.eventWatcher.ConfigSaved += (o, e) => this.ReloadConfigDataAsync();
            this.eventWatcher.EventsSkipped += (o, e) => this.ReloadConfigDataAsync();

            this.connectionsWatcher = connectionsWatcherFactory.CreateConnectionsWatcher(this.apiClient);
            this.connectionsWatcher.TotalConnectionStatsChanged += (o, e) => this.OnTotalConnectionStatsChanged(e.TotalConnectionStats);

            this._folders = new SyncThingFolderManager(this.apiClient, this.eventWatcher, TimeSpan.FromMinutes(10));
            this._transferHistory = new SyncThingTransferHistory(this.eventWatcher, this._folders);
            this._debugFacilities = new SyncThingDebugFacilitiesManager(this.apiClient);

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
            var apiClient = this.apiClient.Value;
            if (this.State != SyncThingState.Running || apiClient == null)
                return;

            // Syncthing can stop so quickly that it doesn't finish sending the response to us
            try
            {
                await apiClient.ShutdownAsync();
            }
            catch (HttpRequestException)
            { }

            this.SetState(SyncThingState.Stopping);
        }

        public async Task StopAndWaitAsync()
        {
            var apiClient = this.apiClient.Value;
            if (this.State != SyncThingState.Running || apiClient == null)
                return;

            var tcs = new TaskCompletionSource<object>();
            EventHandler<SyncThingStateChangedEventArgs> stateChangedHandler = (o, e) =>
            {
                if (e.NewState == SyncThingState.Stopped)
                    tcs.TrySetResult(null);
                else if (e.NewState != SyncThingState.Stopping)
                    tcs.TrySetException(new Exception($"Failed to stop Syncthing: Went to state {e.NewState} instead"));
            };
            this.StateChanged += stateChangedHandler;

            // Syncthing can stop so quickly that it doesn't finish sending the response to us
            try
            {
                await apiClient.ShutdownAsync();
            }
            catch (HttpRequestException)
            { }

            this.SetState(SyncThingState.Stopping);

            await tcs.Task;
            this.StateChanged -= stateChangedHandler;
        }

        public async Task RestartAsync()
        {
            if (this.State != SyncThingState.Running)
                return;

            // Syncthing can stop so quickly that it doesn't finish sending the response to us
            try
            {
                await this.apiClient.Value.RestartAsync();
            }
            catch (HttpRequestException)
            {
            }
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

                // Not entirely sure where this condition comes from...
                if (this._state == SyncThingState.Stopped && state == SyncThingState.Stopping)
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
                // StopApiClients acquires the correct locks, and aborts the CTS
                this.StopApiClients();
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
                var msg = $"RestEase Error. StatusCode: {e.StatusCode}. Content: {e.Content}. Reason: {e.ReasonPhrase}";
                logger.Error(msg, e);
                throw new SyncThingDidNotStartCorrectlyException(msg, e);
            }
            catch (HttpRequestException e)
            {
                var msg = $"HttpRequestException while starting Syncthing: {e.Message}";
                logger.Error(msg, e);
                throw new SyncThingDidNotStartCorrectlyException(msg, e);
            }
            catch (Exception e)
            {
                logger.Error("Error starting Syncthing API", e);
                throw;
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
            this.processRunner.CommandLineFlags = this.SyncthingCommandLineFlags;
            this.processRunner.EnvironmentalVariables = this.SyncthingEnvironmentalVariables;
            this.processRunner.DebugFacilities = this.DebugFacilities.DebugFacilities.Where(x => x.IsEnabled).Select(x => x.Name).ToList();
            this.processRunner.DenyUpgrade = this.SyncthingDenyUpgrade;
            this.processRunner.SyncthingPriorityLevel = this.SyncthingPriorityLevel;
            this.processRunner.HideDeviceIds = this.SyncthingHideDeviceIds;

            var isRestart = (this.State == SyncThingState.Restarting);
            this.SetState(SyncThingState.Starting);

            // Catch restart cases, and re-start the API
            // This isn't ideal, as we don't get to nicely propagate any exceptions to the UI
            if (isRestart)
            {
                try
                {
                    await this.StartClientAsync();
                }
                catch (SyncThingDidNotStartCorrectlyException)
                {
                    // We've already logged this
                }
            }
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
            cancellationToken.ThrowIfCancellationRequested();
            var apiClient = this.apiClient.GetAsserted();

            var syncthingVersionTask = apiClient.FetchVersionAsync();
            var systemInfoTask = apiClient.FetchSystemInfoAsync();

            await Task.WhenAll(syncthingVersionTask, systemInfoTask);

            this.systemInfo = systemInfoTask.Result;
            var syncthingVersion = syncthingVersionTask.Result;

            this.Version = new SyncThingVersionInformation(syncthingVersion.Version, syncthingVersion.LongVersion);
            
            cancellationToken.ThrowIfCancellationRequested();

            var debugFacilitiesLoadTask = this._debugFacilities.LoadAsync(this.Version.ParsedVersion);
            var configDataLoadTask = this.LoadConfigDataAsync(this.systemInfo.Tilde, false, cancellationToken);

            await Task.WhenAll(debugFacilitiesLoadTask, configDataLoadTask);

            cancellationToken.ThrowIfCancellationRequested();
            
            this.StartedTime = DateTime.UtcNow;
            this.IsDataLoaded = true;
            this.OnDataLoaded();
        }

        private async Task LoadConfigDataAsync(string tilde, bool isReload, CancellationToken cancellationToken)
        {
            var apiClient = this.apiClient.GetAsserted();

            var configTask = apiClient.FetchConfigAsync();
            var connectionsTask = apiClient.FetchConnectionsAsync();
            cancellationToken.ThrowIfCancellationRequested();

            await Task.WhenAll(configTask, connectionsTask);

            cancellationToken.ThrowIfCancellationRequested();

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

            if (isReload)
                await this._folders.ReloadFoldersAsync(configTask.Result, tilde, cancellationToken);
            else
                await this._folders.LoadFoldersAsync(configTask.Result, tilde, cancellationToken);
        }

        private async void ReloadConfigDataAsync()
        {
            // Shit. We don't know what state any of our folders are in. We'll have to poll them all....
            // Note that we're executing on the ThreadPool here: we don't have a Task route back to the main thread.
            // Any exceptions are ours to manage....

            // HttpRequestException, ApiException, and  OperationCanceledException are more or less expected: Syncthing could shut down
            // at any point

            try
            { 
                await this.LoadConfigDataAsync(this.systemInfo.Tilde, true, CancellationToken.None);
            }
            catch (HttpRequestException)
            { }
            catch (OperationCanceledException)
            { }
            catch (ApiException)
            { }
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
