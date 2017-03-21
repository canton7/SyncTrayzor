using SyncTrayzor.Syncthing;
using SyncTrayzor.Syncthing.Folders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SyncTrayzor.Services
{
    public interface IWatchedFolderMonitor : IDisposable
    {
        IEnumerable<string> WatchedFolderIDs { get; set; }
        TimeSpan BackoffInterval { get; set; }
        TimeSpan FolderExistenceCheckingInterval { get; set; }
    }

    public class WatchedFolderMonitor : IWatchedFolderMonitor
    {
        // Paths we don't alert Syncthing about
        private static readonly string[] specialPaths = new[] { ".stversions", ".stfolder", "~syncthing~", ".syncthing." };

        private readonly ISyncthingManager syncthingManager;
        private readonly IDirectoryWatcherFactory directoryWatcherFactory;

        private readonly List<DirectoryWatcher> directoryWatchers = new List<DirectoryWatcher>();

        private List<string> _watchedFolders;
        public IEnumerable<string> WatchedFolderIDs
        {
            get => this._watchedFolders;
            set
            {
                if (this._watchedFolders != null && value != null && this._watchedFolders.SequenceEqual(value))
                    return;

                this._watchedFolders = value?.ToList();
                this.Reset();
            }
        }

        public TimeSpan BackoffInterval { get; set; }
        public TimeSpan FolderExistenceCheckingInterval { get; set; }

        public WatchedFolderMonitor(ISyncthingManager syncthingManager, IDirectoryWatcherFactory directoryWatcherFactory)
        {
            this.syncthingManager = syncthingManager;
            this.directoryWatcherFactory = directoryWatcherFactory;

            this.syncthingManager.Folders.FoldersChanged += this.FoldersChanged;
            this.syncthingManager.Folders.SyncStateChanged += this.FolderSyncStateChanged;
            this.syncthingManager.StateChanged += this.StateChanged;
        }

        private void FoldersChanged(object sender, EventArgs e)
        {
            this.Reset();
        }

        private void FolderSyncStateChanged(object sender, FolderSyncStateChangedEventArgs e)
        {
            // Don't monitor failed folders, and pick up on unfailed folders
            if (e.SyncState == FolderSyncState.Error || e.PrevSyncState == FolderSyncState.Error)
                this.Reset();
        }

        private void StateChanged(object sender, SyncthingStateChangedEventArgs e)
        {
            this.Reset();
        }

        private void Reset()
        {
            // Has everything loaded yet?
            if (this._watchedFolders == null)
                return;

            foreach (var watcher in directoryWatchers)
            {
                watcher.Dispose();
            }
            this.directoryWatchers.Clear();

            if (this.syncthingManager.State != SyncthingState.Running)
                return;

            var folders = this.syncthingManager.Folders.FetchAll();
            if (folders == null)
                return; // Folders haven't yet loaded

            foreach (var folder in folders)
            {
                if (!this._watchedFolders.Contains(folder.FolderId) || folder.SyncState == FolderSyncState.Error)
                    continue;

                var watcher = this.directoryWatcherFactory.Create(folder.Path, this.BackoffInterval, this.FolderExistenceCheckingInterval);
                watcher.PreviewDirectoryChanged += (o, e) => e.Cancel = this.WatcherPreviewDirectoryChanged(folder, e);
                watcher.DirectoryChanged += (o, e) => this.WatcherDirectoryChanged(folder, e.SubPath);

                this.directoryWatchers.Add(watcher);
            }
        }

        // Returns true to cancel
        private bool WatcherPreviewDirectoryChanged(Folder folder, PreviewDirectoryChangedEventArgs e)
        {
            var subPath = e.SubPath;

            // Is it a syncthing temp/special path?
            if (specialPaths.Any(x => subPath.StartsWith(x)))
                return true;

            if (folder.SyncState == FolderSyncState.Syncing || folder.IsSyncingPath(subPath))
                return true;

            return false;
        }

        private void WatcherDirectoryChanged(Folder folder, string subPath)
        {
            // If it's currently syncing, then don't refresh it
            if (folder.SyncState == FolderSyncState.Syncing)
                return;

            this.syncthingManager.ScanAsync(folder.FolderId, subPath.Replace(Path.DirectorySeparatorChar, '/'));
        }

        public void Dispose()
        {
            this.syncthingManager.Folders.FoldersChanged -= this.FoldersChanged;
            this.syncthingManager.Folders.SyncStateChanged -= this.FolderSyncStateChanged;
            this.syncthingManager.StateChanged -= this.StateChanged;
        }
    }
}
