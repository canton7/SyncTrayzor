using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        string ExecutablePath { get; set; }
        Uri Address { get; set; }
        DateTime? StartedAt { get; }
        Dictionary<string, Folder> Folders { get; }

        void Start();
        Task StopAsync();
        void Kill();
        Task ScanAsync(string folderId, string subPath);
    }

    public class SyncThingManager : ISyncThingManager
    {
        private readonly SynchronizedEventDispatcher eventDispatcher;
        private readonly ISyncThingProcessRunner processRunner;
        private readonly ISyncThingApiClient apiClient;
        private readonly ISyncThingEventWatcher eventWatcher;
        private readonly ISyncThingConnectionsWatcher connectionsWatcher;
        private readonly string apiKey;

        public DateTime? StartedAt { get; private set; }

        public SyncThingState State { get; private set; }
        public bool IsDataLoaded { get; private set; }
        public event EventHandler DataLoaded;
        public event EventHandler<SyncThingStateChangedEventArgs> StateChanged;
        public event EventHandler<MessageLoggedEventArgs> MessageLogged;
        public event EventHandler<FolderSyncStateChangeEventArgs> FolderSyncStateChanged;
        public SyncThingConnectionStats TotalConnectionStats { get; private set; }
        public event EventHandler<ConnectionStatsChangedEventArgs> TotalConnectionStatsChanged;

        public string ExecutablePath { get; set; }
        public Uri Address { get; set; }

        public Dictionary<string, Folder> Folders { get; private set; }

        public SyncThingManager(
            ISyncThingProcessRunner processRunner,
            ISyncThingApiClient apiClient,
            ISyncThingEventWatcher eventWatcher,
            ISyncThingConnectionsWatcher connectionsWatcher)
        {
            this.Folders = new Dictionary<string, Folder>();

            this.eventDispatcher = new SynchronizedEventDispatcher(this);
            this.processRunner = processRunner;
            this.apiClient = apiClient;
            this.eventWatcher = eventWatcher;
            this.connectionsWatcher = connectionsWatcher;

            this.processRunner.ProcessStopped += (o, e) => this.SetState(SyncThingState.Stopped);
            this.processRunner.MessageLogged += (o, e) => this.OnMessageLogged(e.LogMessage);

            this.eventWatcher.StartupComplete += (o, e) => this.StartupComplete();
            this.eventWatcher.SyncStateChanged += (o, e) => this.OnSyncStateChanged(e);

            this.connectionsWatcher.TotalConnectionStatsChanged += (o, e) => this.OnTotalConnectionStatsChanged(e.TotalConnectionStats);

            this.apiKey = "abc123";
            this.processRunner.ApiKey = apiKey;
        }

        public void Start()
        {
            this.apiClient.SetConnectionDetails(this.Address, this.apiKey);
            this.processRunner.HostAddress = this.Address.ToString();
            this.processRunner.ExecutablePath = this.ExecutablePath;

            this.processRunner.Start();
            this.SetState(SyncThingState.Starting);
        }

        public Task StopAsync()
        {
            this.SetState(SyncThingState.Stopping);
            return this.apiClient.ShutdownAsync();
        }

        public void Kill()
        {
            this.processRunner.Kill();
            this.SetState(SyncThingState.Stopped);
        }

        public Task ScanAsync(string folderId, string subPath)
        {
            return this.apiClient.ScanAsync(folderId, subPath);
        }

        private void SetState(SyncThingState state)
        {
            if (state == this.State)
                return;

            var oldState = this.State;
            this.State = state;

            this.UpdateWatchersState(state);

            this.eventDispatcher.Raise(this.StateChanged, new SyncThingStateChangedEventArgs(oldState, state));
        }

        private void UpdateWatchersState(SyncThingState state)
        {
            var running = (state == SyncThingState.Starting || state == SyncThingState.Running);
            this.eventWatcher.Running = running;
            this.connectionsWatcher.Running = running;
        }

        private async void StartupComplete()
        {
            this.StartedAt = DateTime.UtcNow;
            this.SetState(SyncThingState.Running);

            var configTask = this.apiClient.FetchConfigAsync();
            var systemTask = this.apiClient.FetchSystemInfoAsync();
            await Task.WhenAll(configTask, systemTask);

            var tilde = systemTask.Result.Tilde;

            this.Folders = configTask.Result.Folders.ToDictionary(x => x.ID, x =>
            {
                var path = x.Path.Replace("~", tilde);
                return new Folder(x.ID, path);
            });

            this.OnDataLoaded();
            this.IsDataLoaded = true;
        }

        private void OnMessageLogged(string logMessage)
        {
            this.eventDispatcher.Raise(this.MessageLogged, new MessageLoggedEventArgs(logMessage));
        }

        private void OnSyncStateChanged(SyncStateChangedEventArgs e)
        {
            Folder folder;
            if (!this.Folders.TryGetValue(e.FolderId, out folder))
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

        public void Dispose()
        {
            this.processRunner.Dispose();
        }
    }
}
