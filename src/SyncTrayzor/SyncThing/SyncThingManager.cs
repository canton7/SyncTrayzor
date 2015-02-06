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
        private readonly string apiKey;

        public DateTime? StartedAt { get; private set; }

        public SyncThingState State { get; private set; }
        public bool IsDataLoaded { get; private set; }
        public event EventHandler DataLoaded;
        public event EventHandler<SyncThingStateChangedEventArgs> StateChanged;
        public event EventHandler<MessageLoggedEventArgs> MessageLogged;
        public event EventHandler<FolderSyncStateChangeEventArgs> FolderSyncStateChanged;

        public string ExecutablePath { get; set; }
        public Uri Address { get; set; }

        public Dictionary<string, Folder> Folders { get; private set; }

        public SyncThingManager(
            ISyncThingProcessRunner processRunner,
            ISyncThingApiClient apiClient,
            ISyncThingEventWatcher eventWatcher)
        {
            this.Folders = new Dictionary<string, Folder>();

            this.eventDispatcher = new SynchronizedEventDispatcher(this);
            this.processRunner = processRunner;
            this.apiClient = apiClient;
            this.eventWatcher = eventWatcher;

            this.processRunner.ProcessStopped += (o, e) => this.SetState(SyncThingState.Stopped);
            this.processRunner.MessageLogged += (o, e) => this.OnMessageLogged(e.LogMessage);

            this.eventWatcher.StartupComplete += (o, e) => this.StartupComplete();
            this.eventWatcher.SyncStateChanged += (o, e) => this.OnSyncStateChanged(e);

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

            this.UpdateEventWatcherState(state);

            this.eventDispatcher.Raise(this.StateChanged, new SyncThingStateChangedEventArgs(oldState, state));
        }

        private void UpdateEventWatcherState(SyncThingState state)
        {
            this.eventWatcher.Running = (state == SyncThingState.Starting || state == SyncThingState.Running);
        }

        private async void StartupComplete()
        {
            this.StartedAt = DateTime.UtcNow;
            this.SetState(SyncThingState.Running);

            var config = await this.apiClient.FetchConfigAsync();
            this.Folders = config.Folders.ToDictionary(x => x.ID, x => new Folder(x.ID, x.Path));

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
