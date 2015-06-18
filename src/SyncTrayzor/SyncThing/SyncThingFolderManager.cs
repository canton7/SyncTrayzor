using NLog;
using RestEase;
using SyncTrayzor.SyncThing.ApiClient;
using SyncTrayzor.SyncThing.EventWatcher;
using SyncTrayzor.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public interface ISyncThingFolderManager
    {
        bool TryFetchById(string folderId, out Folder folder);
        IReadOnlyCollection<Folder> FetchAll();

        event EventHandler FoldersChanged;
        event EventHandler<FolderSyncStateChangeEventArgs> SyncStateChanged;
    }

    public class SyncThingFolderManager : ISyncThingFolderManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly SynchronizedEventDispatcher eventDispatcher;

        private readonly SynchronizedTransientWrapper<ISyncThingApiClient> apiClient;
        private readonly ISyncThingEventWatcher eventWatcher;
        private readonly TimeSpan ignoresFetchTimeout;

        public event EventHandler FoldersChanged;
        public event EventHandler<FolderSyncStateChangeEventArgs> SyncStateChanged;

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

        public SyncThingFolderManager(
            SynchronizedTransientWrapper<ISyncThingApiClient> apiClient,
            ISyncThingEventWatcher eventWatcher,
            TimeSpan ignoresFetchTimeout)
        {
            this.eventDispatcher = new SynchronizedEventDispatcher(this);
            this.apiClient = apiClient;
            this.ignoresFetchTimeout = ignoresFetchTimeout;

            this.eventWatcher = eventWatcher;
            this.eventWatcher.SyncStateChanged += (o, e) => this.FolderSyncStateChanged(e);
            this.eventWatcher.ItemStarted += (o, e) => this.ItemStarted(e.Folder, e.Item);
            this.eventWatcher.ItemFinished += (o, e) => this.ItemFinished(e.Folder, e.Item);
            this.eventWatcher.EventsSkipped += (o, e) => this.EventsSkipped();
        }

        public bool TryFetchById(string folderId, out Folder folder)
        {
            return this.folders.TryGetValue(folderId, out folder);
        }

        public IReadOnlyCollection<Folder> FetchAll()
        {
            return new List<Folder>(this.folders.Values).AsReadOnly();
        }

        public async Task ReloadIgnoresAsync(string folderId)
        {
            Folder folder;
            if (!this.folders.TryGetValue(folderId, out folder))
                return;

            var ignores = await this.apiClient.Value.FetchIgnoresAsync(folderId);
            folder.Ignores = new FolderIgnores(ignores.IgnorePatterns, ignores.RegexPatterns);
        }

        public async Task LoadFoldersAsync(Config config, SystemInfo systemInfo, CancellationToken cancellationToken)
        {
            // If the folder is invalid for any reason, we'll ignore it.
            // Again, there's the potential for duplicate folder IDs (if the user's been fiddling their config). 
            // In this case, there's nothing really sensible we can do. Just pick one of them :)
            var folderConstructionTasks = config.Folders
                .Where(x => String.IsNullOrWhiteSpace(x.Invalid))
                .DistinctBy(x => x.ID)
                .Select(async folder =>
                {
                    var ignores = await this.FetchFolderIgnoresAsync(folder.ID, cancellationToken);
                    var syncState = await this.FetchFolderSyncStateAsync(folder.ID, cancellationToken);
                    var path = folder.Path;
                    if (path.StartsWith("~"))
                        path = Path.Combine(systemInfo.Tilde, path.Substring(1).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                    return new Folder(folder.ID, path, syncState, new FolderIgnores(ignores.IgnorePatterns, ignores.RegexPatterns));
                });

            cancellationToken.ThrowIfCancellationRequested();

            var folders = await Task.WhenAll(folderConstructionTasks);
            this.folders = new ConcurrentDictionary<string, Folder>(folders.Select(x => new KeyValuePair<string, Folder>(x.FolderId, x)));

            this.OnFoldersChanged();
        }

        private async Task<Ignores> FetchFolderIgnoresAsync(string folderId, CancellationToken cancellationToken)
        {
            // Until startup is complete, these can return a 500.
            // There's no sensible way to determine when startup *is* complete, so we just have to keep trying...

            // Again, there's the possibility that we've just abort the API...
            ISyncThingApiClient apiClient;
            lock (this.apiClient.LockObject)
            {
                cancellationToken.ThrowIfCancellationRequested();
                apiClient = this.apiClient.UnsynchronizedValue;
                if (apiClient == null)
                    throw new InvalidOperationException("ApiClient must not be null");
            }

            Ignores ignores;
            var startedTime = DateTime.UtcNow;
            while (true)
            {
                try
                {
                    ignores = await apiClient.FetchIgnoresAsync(folderId);
                    // No need to log: ApiClient did that for us
                    break;
                }
                catch (ApiException e)
                {
                    logger.Debug("Attempting to fetch folder {0}, but received status {1}", folderId, e.StatusCode);
                    if (e.StatusCode != HttpStatusCode.InternalServerError)
                        throw;
                }

                if (DateTime.UtcNow - startedTime > this.ignoresFetchTimeout)
                    throw new SyncThingDidNotStartCorrectlyException(String.Format("Unable to fetch ignores for folder {0}. Syncthing returned 500 after {1}", folderId, DateTime.UtcNow - startedTime));

                await Task.Delay(1000, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return ignores;
        }

        private async Task<FolderSyncState> FetchFolderSyncStateAsync(string folderId, CancellationToken cancellationToken)
        {
            var status = await this.apiClient.Value.FetchFolderStatusAsync(folderId, cancellationToken);
            return status.State == "syncing" ? FolderSyncState.Syncing : FolderSyncState.Idle;
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

        private void FolderSyncStateChanged(SyncStateChangedEventArgs e)
        {
            Folder folder;
            if (!this.folders.TryGetValue(e.FolderId, out folder))
                return; // We don't know about this folder

            folder.SyncState = e.SyncState;

            this.OnSyncStateChanged(folder, e.PrevSyncState, e.SyncState);
        }

        private async void EventsSkipped()
        {
            // Shit. We don't know what state any of our folders are in. We'll have to poll them all....
            var folders = this.FetchAll();
            var updateTasks = folders.Select(async folder =>
            {
                var newState = await this.FetchFolderSyncStateAsync(folder.FolderId, CancellationToken.None);
                var oldState = folder.SyncState;

                if (oldState != newState)
                {
                    folder.SyncState = newState;
                    this.OnSyncStateChanged(folder, oldState, newState);
                }
            });

            await Task.WhenAll(updateTasks);
        }

        private void OnFoldersChanged()
        {
            this.eventDispatcher.Raise(this.FoldersChanged);
        }

        private void OnSyncStateChanged(Folder folder, FolderSyncState prevSyncState, FolderSyncState newSyncState)
        {
            this.eventDispatcher.Raise(this.SyncStateChanged, new FolderSyncStateChangeEventArgs(folder, prevSyncState, newSyncState));
        }
    }
}
