using NLog;
using SyncTrayzor.SyncThing.Api;
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
        event EventHandler ProcessExitedWithError;

        string ExecutablePath { get; set; }
        string ApiKey { get; set; }
        Uri Address { get; set; }
        DateTime? StartedAt { get; }
        Dictionary<string, Folder> Folders { get; }
        SyncthingVersion Version { get; }

        void Start();
        Task StopAsync();
        void Kill();
        void KillAllSyncthingProcesses();
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

        public DateTime? StartedAt { get; private set; }

        public SyncThingState State { get; private set; }
        public bool IsDataLoaded { get; private set; }
        public event EventHandler DataLoaded;
        public event EventHandler<SyncThingStateChangedEventArgs> StateChanged;
        public event EventHandler<MessageLoggedEventArgs> MessageLogged;
        public event EventHandler<FolderSyncStateChangeEventArgs> FolderSyncStateChanged;
        public SyncThingConnectionStats TotalConnectionStats { get; private set; }
        public event EventHandler<ConnectionStatsChangedEventArgs> TotalConnectionStatsChanged;
        public event EventHandler ProcessExitedWithError;

        public string ExecutablePath { get; set; }
        public string ApiKey { get; set; }
        public Uri Address { get; set; }

        public Dictionary<string, Folder> Folders { get; private set; }
        public SyncthingVersion Version { get; private set; }

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

            this.processRunner.ProcessStopped += (o, e) => this.ProcessStopped(e.ExitStatus);
            this.processRunner.MessageLogged += (o, e) => this.OnMessageLogged(e.LogMessage);

            this.eventWatcher.StartupComplete += (o, e) => { var t = this.StartupCompleteAsync(); };
            this.eventWatcher.SyncStateChanged += (o, e) => this.OnSyncStateChanged(e);
            this.eventWatcher.ItemStarted += (o, e) => this.ItemStarted(e.Folder, e.Item);
            this.eventWatcher.ItemFinished += (o, e) => this.ItemFinished(e.Folder, e.Item);

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
            this.SetState(SyncThingState.Stopping);
            return this.apiClient.ShutdownAsync();
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

        public Task ScanAsync(string folderId, string subPath)
        {
            return this.apiClient.ScanAsync(folderId, subPath);
        }

        public async Task ReloadIgnoresAsync(string folderId)
        {
            Folder folder;
            if (!this.Folders.TryGetValue(folderId, out folder))
                return;

            var ignores = await this.apiClient.FetchIgnoresAsync(folderId);
            folder.Ignores = new FolderIgnores(ignores.IgnorePatterns, ignores.RegexPatterns);
        }

        private void SetState(SyncThingState state)
        {
            if (state == this.State)
                return;

            // We really need a proper state machine here....
            // There's a race if Syncthing can't start because the database is locked by another process on the same port
            // In this case, we see the process as having failed, but the event watcher chimes in a split-second later with the 'Started' event.
            // This runs the risk of transitioning us from Stopped -> Starting -> Stopepd -> Running, which is bad news for everyone
            // So, get around this by enforcing strict state transitions.
            if (this.State == SyncThingState.Stopped && state == SyncThingState.Running)
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

        private void ProcessStopped(SyncThingExitStatus exitStatus)
        {
            this.SetState(SyncThingState.Stopped);
            if (exitStatus == SyncThingExitStatus.Error)
                this.OnProcessExitedWithError();
        }

        private async Task StartupCompleteAsync()
        {
            this.StartedAt = DateTime.UtcNow;
            this.SetState(SyncThingState.Running);

            var configTask = this.apiClient.FetchConfigAsync();
            var systemTask = this.apiClient.FetchSystemInfoAsync();
            var versionTask = this.apiClient.FetchVersionAsync();
            await Task.WhenAll(configTask, systemTask, versionTask);

            var tilde = systemTask.Result.Tilde;

            var folderConstructionTasks = configTask.Result.Folders.Select(async folder =>
            {
                var ignores = await this.apiClient.FetchIgnoresAsync(folder.ID);
                var path = folder.Path.Replace("~", tilde);
                return new Folder(folder.ID, path, new FolderIgnores(ignores.IgnorePatterns, ignores.RegexPatterns));
            });

            this.Folders = (await Task.WhenAll(folderConstructionTasks)).ToDictionary(x => x.FolderId, x => x);
            this.Version = versionTask.Result;

            this.OnDataLoaded();
            this.IsDataLoaded = true;
        }

        private void ItemStarted(string folderId, string item)
        {
            Folder folder;
            if (!this.Folders.TryGetValue(folderId, out folder))
                return; // Don't know about it

            folder.SyncthingPaths.Add(item);
        }

        private void ItemFinished(string folderId, string item)
        {
            Folder folder;
            if (!this.Folders.TryGetValue(folderId, out folder))
                return; // Don't know about it

            folder.SyncthingPaths.Remove(item);
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
