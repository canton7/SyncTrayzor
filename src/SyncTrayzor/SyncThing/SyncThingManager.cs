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
        event EventHandler DataLoaded;
        event EventHandler<SyncThingStateChangedEventArgs> StateChanged;
        event EventHandler<MessageLoggedEventArgs> MessageLogged;
        event EventHandler<SyncStateChangedEventArgs> SyncStateChanged;

        string ExecutablePath { get; set; }
        string Address { get; set; }

        Dictionary<string, Folder> Folders { get; }

        void Start();
        Task StopAsync();
        void Kill();
    }

    public class SyncThingManager : ISyncThingManager
    {
        private readonly SynchronizedEventDispatcher eventDispatcher;
        private readonly ISyncThingProcessRunner processRunner;
        private readonly ISyncThingApiClient apiClient;
        private readonly ISyncThingEventWatcher eventWatcher;
        private readonly string apiKey;

        private DateTime startedAt = DateTime.MinValue;

        public SyncThingState State { get; private set; }
        public event EventHandler DataLoaded;
        public event EventHandler<SyncThingStateChangedEventArgs> StateChanged;
        public event EventHandler<MessageLoggedEventArgs> MessageLogged;
        public event EventHandler<SyncStateChangedEventArgs> SyncStateChanged;

        public string ExecutablePath { get; set; }
        public string Address { get; set; }

        public Dictionary<string, Folder> Folders { get; private set; }

        public SyncThingManager(
            ISyncThingProcessRunner processRunner,
            ISyncThingApiClient apiClient,
            ISyncThingEventWatcher eventWatcher)
        {
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
            this.apiClient.SetConnectionDetails(new Uri(this.Address), this.apiKey);
            this.processRunner.HostAddress = this.Address;
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

        private void SetState(SyncThingState state)
        {
            if (state == this.State)
                return;

            var oldState = this.State;
            this.State = state;

            this.UpdateEventWatcherState(this.State);

            this.eventDispatcher.Raise(this.StateChanged, new SyncThingStateChangedEventArgs(oldState, state));
        }

        private void UpdateEventWatcherState(SyncThingState state)
        {
            this.eventWatcher.Running = (state == SyncThingState.Starting || state == SyncThingState.Running);
        }

        private async void StartupComplete()
        {
            this.startedAt = DateTime.UtcNow;
            this.SetState(SyncThingState.Running);

            var config = await this.apiClient.FetchConfigAsync();
            this.Folders = config.Folders.ToDictionary(x => x.ID, x => new Folder(x.ID, x.Path));

            this.OnDataLoaded();
        }

        private void OnMessageLogged(string logMessage)
        {
            this.eventDispatcher.Raise(this.MessageLogged, new MessageLoggedEventArgs(logMessage));
        }

        private void OnSyncStateChanged(SyncStateChangedEventArgs e)
        {
            // There's a 'synced' event straight after starting - ignore it
            if (DateTime.UtcNow - this.startedAt < TimeSpan.FromSeconds(60))
                return;

            this.eventDispatcher.Raise(this.SyncStateChanged, e);
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
