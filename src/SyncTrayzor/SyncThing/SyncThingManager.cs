using NLog;
using SyncTrayzor.SyncThing.Api;
using SyncTrayzor.SyncThing.EventWatcher;
using SyncTrayzor.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        event EventHandler<FolderSyncStateChangeEventArgs> FolderSyncStateChanged;
        SyncThingConnectionStats TotalConnectionStats { get; }
        event EventHandler<ConnectionStatsChangedEventArgs> TotalConnectionStatsChanged;
        event EventHandler ProcessExitedWithError;
        event EventHandler<DeviceConnectedEventArgs> DeviceConnected;
        event EventHandler<DeviceDisconnectedEventArgs> DeviceDisconnected;

        string ExecutablePath { get; set; }
        string ApiKey { get; set; }
        Uri Address { get; set; }
        string SyncthingTraceFacilities { get; set; }
        string SyncthingCustomHomeDir { get; set; }
        bool SyncthingDenyUpgrade { get; set; }
        DateTime StartedTime { get; }
        DateTime LastConnectivityEventTime { get; }
        SyncthingVersion Version { get; }

        void Start();
        Task StopAsync();
        Task RestartAsync();
        void Kill();
        void KillAllSyncthingProcesses();

        bool TryFetchFolderById(string folderId, out Folder folder);
        IReadOnlyCollection<Folder> FetchAllFolders();

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
        private readonly ISyncThingApiClient apiClient;
        private readonly ISyncThingEventWatcher eventWatcher;
        private readonly ISyncThingConnectionsWatcher connectionsWatcher;

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
        public event EventHandler<FolderSyncStateChangeEventArgs> FolderSyncStateChanged;
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
        public Uri Address { get; set; }
        public string SyncthingCustomHomeDir { get; set; }
        public string SyncthingTraceFacilities { get; set; }
        public bool SyncthingDenyUpgrade { get; set; }

        // Folders is a ConcurrentDictionary, which suffices for most access
        // However, it is sometimes set outright (in the case of an initial load or refresh), so we need this lock
        // to create a memory barrier. The lock is only used when setting/fetching the field, not when accessing the
        // Folders dictionary itself.
        private readonly object foldersLock = new object();
        private ConcurrentDictionary<string, Folder> _folders = new ConcurrentDictionary<string, Folder>();
        private ConcurrentDictionary<string, Folder> folders
        {
            get { lock (this.foldersLock) { return this._folders; } }
            set { lock (this.foldersLock) { this._folders = value; } }
        }

        private readonly object devicesLock = new object();
        private ConcurrentDictionary<string, Device> _devices = new ConcurrentDictionary<string, Device>();
        public ConcurrentDictionary<string, Device> devices
        {
            get { lock (this.devicesLock) { return this._devices; } }
            set { lock (this.devicesLock) this._devices = value; }
        }

        public SyncthingVersion Version { get; private set; }

        public SyncThingManager(
            ISyncThingProcessRunner processRunner,
            ISyncThingApiClient apiClient,
            ISyncThingEventWatcher eventWatcher,
            ISyncThingConnectionsWatcher connectionsWatcher)
        {
            this.StartedTime = DateTime.MinValue;
            this.LastConnectivityEventTime = DateTime.MinValue;

            this.eventDispatcher = new SynchronizedEventDispatcher(this);
            this.processRunner = processRunner;
            this.apiClient = apiClient;
            this.eventWatcher = eventWatcher;
            this.connectionsWatcher = connectionsWatcher;

            this.processRunner.ProcessStopped += (o, e) => this.ProcessStopped(e.ExitStatus);
            this.processRunner.MessageLogged += (o, e) => this.OnMessageLogged(e.LogMessage);

            this.eventWatcher.StartupComplete += (o, e) => { var t = this.StartupCompleteAsync(); };
            this.eventWatcher.SyncStateChanged += (o, e) => this.OnFolderSyncStateChanged(e);
            this.eventWatcher.ItemStarted += (o, e) => this.ItemStarted(e.Folder, e.Item);
            this.eventWatcher.ItemFinished += (o, e) => this.ItemFinished(e.Folder, e.Item);
            this.eventWatcher.DeviceConnected += (o, e) => this.OnDeviceConnected(e);
            this.eventWatcher.DeviceDisconnected += (o, e) => this.OnDeviceDisconnected(e);

            this.connectionsWatcher.TotalConnectionStatsChanged += (o, e) => this.OnTotalConnectionStatsChanged(e.TotalConnectionStats);
        }

        public void Start()
        {
            try
            {
                this.apiClient.SetConnectionDetails(this.Address, this.ApiKey);
                this.processRunner.ApiKey = this.ApiKey;
                this.processRunner.HostAddress = this.Address.ToString();
                this.processRunner.ExecutablePath = this.ExecutablePath;
                this.processRunner.CustomHomeDir = this.SyncthingCustomHomeDir;
                this.processRunner.Traces = this.SyncthingTraceFacilities;
                this.processRunner.DenyUpgrade = this.SyncthingDenyUpgrade;

                this.processRunner.Start();
                this.SetState(SyncThingState.Starting);
            }
            catch (Exception e)
            {
                logger.Error("Error starting SyncThing", e);
                throw;
            }
        }

        public Task StopAsync()
        {
            if (this.State != SyncThingState.Running)
                return Task.FromResult(false);

            this.SetState(SyncThingState.Stopping);
            return this.apiClient.ShutdownAsync();
        }

        public Task RestartAsync()
        {
            if (this.State != SyncThingState.Running)
                return Task.FromResult(false);

            return this.apiClient.RestartAsync();
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

        public bool TryFetchFolderById(string folderId, out Folder folder)
        {
            return this.folders.TryGetValue(folderId, out folder);
        }

        public IReadOnlyCollection<Folder> FetchAllFolders()
        {
            return new List<Folder>(this.folders.Values).AsReadOnly();
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
            return this.apiClient.ScanAsync(folderId, subPath);
        }

        public async Task ReloadIgnoresAsync(string folderId)
        {
            Folder folder;
            if (!this.folders.TryGetValue(folderId, out folder))
                return;

            var ignores = await this.apiClient.FetchIgnoresAsync(folderId);
            folder.Ignores = new FolderIgnores(ignores.IgnorePatterns, ignores.RegexPatterns);
        }

        private void SetState(SyncThingState state)
        {
            SyncThingState oldState;
            lock (this.stateLock)
            {
                if (state == this._state)
                    return;

                oldState = this._state;

                // We really need a proper state machine here....
                // There's a race if Syncthing can't start because the database is locked by another process on the same port
                // In this case, we see the process as having failed, but the event watcher chimes in a split-second later with the 'Started' event.
                // This runs the risk of transitioning us from Stopped -> Starting -> Stopepd -> Running, which is bad news for everyone
                // So, get around this by enforcing strict state transitions.
                if (this._state == SyncThingState.Stopped && state == SyncThingState.Running)
                    return;

                this._state = state;
            }

            this.UpdateWatchersState(state);
            this.eventDispatcher.Raise(this.StateChanged, new SyncThingStateChangedEventArgs(oldState, state));
        }

        private void UpdateWatchersState(SyncThingState state)
        {
            var running = (state == SyncThingState.Starting || state == SyncThingState.Running);
            this.eventWatcher.Running = running;
            this.connectionsWatcher.Running = running;
        }

        private void ProcessStopped(SyncThingExitStatus exitStatus)
        {
            this.SetState(SyncThingState.Stopped);
            if (exitStatus == SyncThingExitStatus.Error)
                this.OnProcessExitedWithError();
        }

        private async Task StartupCompleteAsync()
        {
            this.SetState(SyncThingState.Running);

            var configTask = this.apiClient.FetchConfigAsync();
            var systemTask = this.apiClient.FetchSystemInfoAsync();
            var versionTask = this.apiClient.FetchVersionAsync();
            var connectionsTask = this.apiClient.FetchConnectionsAsync();
            await Task.WhenAll(configTask, systemTask, versionTask, connectionsTask);

            this.devices = new ConcurrentDictionary<string, Device>(configTask.Result.Devices.Select(device =>
            {
                var deviceObj = new Device(device.DeviceID, device.Name);
                ItemConnectionData connectionData;
                if (connectionsTask.Result.DeviceConnections.TryGetValue(device.DeviceID, out connectionData))
                    deviceObj.SetConnected(connectionData.Address);
                return new KeyValuePair<string, Device>(device.DeviceID, deviceObj);
            }));

            var tilde = systemTask.Result.Tilde;

            var folderConstructionTasks = configTask.Result.Folders.Select(async folder =>
            {
                var ignores = await this.apiClient.FetchIgnoresAsync(folder.ID);
                var path = folder.Path;
                if (path.StartsWith("~"))
                    path = Path.Combine(tilde, path.Substring(1).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                return new Folder(folder.ID, path, new FolderIgnores(ignores.IgnorePatterns, ignores.RegexPatterns));
            });

            var folders = await Task.WhenAll(folderConstructionTasks);
            this.folders = new ConcurrentDictionary<string, Folder>(folders.Select(x => new KeyValuePair<string, Folder>(x.FolderId, x)));

            this.Version = versionTask.Result;

            this.OnDataLoaded();
            this.StartedTime = DateTime.UtcNow;
            this.IsDataLoaded = true;
        }

        private void ItemStarted(string folderId, string item)
        {
            Folder folder;
            if (!this.folders.TryGetValue(folderId, out folder))
                return; // Don't know about it

            folder.AddSyncingPath(item);
        }

        private void ItemFinished(string folderId, string item)
        {
            Folder folder;
            if (!this.folders.TryGetValue(folderId, out folder))
                return; // Don't know about it

            folder.RemoveSyncingPath(item);
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

        private void OnFolderSyncStateChanged(SyncStateChangedEventArgs e)
        {
            Folder folder;
            if (!this.folders.TryGetValue(e.FolderId, out folder))
                return; // We don't know about this folder

            folder.SyncState = e.SyncState;

            this.eventDispatcher.Raise(this.FolderSyncStateChanged, new FolderSyncStateChangeEventArgs(folder, e.PrevSyncState, e.SyncState));
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
        }
    }
}
