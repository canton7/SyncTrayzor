using SyncTrayzor.SyncThing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services
{
    public interface IWatchedFolderMonitor
    {
        IEnumerable<string> WatchedFolderIDs { get; set; }
    }

    public class WatchedFolderMonitor : IWatchedFolderMonitor
    {
        private readonly ISyncThingManager syncThingManager;
        private readonly List<DirectoryWatcher> directoryWatchers = new List<DirectoryWatcher>();

        private List<string> _watchedFolders;
        public IEnumerable<string> WatchedFolderIDs
        {
            get { return this._watchedFolders; }
            set
            {
                if (this._watchedFolders != null && value != null && this._watchedFolders.SequenceEqual(value))
                    return;

                this._watchedFolders = value != null ? value.ToList() : null;
                this.Reset();
            }
        }

        public WatchedFolderMonitor(ISyncThingManager syncThingManager)
        {
            this.syncThingManager = syncThingManager;
            this.syncThingManager.DataLoaded += (o, e) => this.Reset();
            this.syncThingManager.StateChanged += (o, e) => this.Reset();
        }

        private void Reset()
        {
            // Has everything loaded yet?
            if (this._watchedFolders == null || this.syncThingManager.Folders == null)
                return;

            foreach (var watcher in directoryWatchers)
            {
                watcher.Dispose();
            }
            this.directoryWatchers.Clear();

            if (this.syncThingManager.State != SyncThingState.Running || !this.syncThingManager.IsDataLoaded)
                return;

            foreach (var watchedFolder in this._watchedFolders)
            {
                Folder folder;
                if (!this.syncThingManager.Folders.TryGetValue(watchedFolder, out folder))
                    continue;

                var watcher = new DirectoryWatcher(folder.Path);
                watcher.DirectoryChanged += (o, e) =>  this.DirectoryChanged(folder.FolderId, e.SubPath);

                this.directoryWatchers.Add(watcher);
            }
        }

        private void DirectoryChanged(string folderId, string subPath)
        {
            Folder folder;
            // If it's currently syncing, then don't refresh it
            if (!this.syncThingManager.Folders.TryGetValue(folderId, out folder) || folder.SyncState == FolderSyncState.Syncing)
                return;

            this.syncThingManager.ScanAsync(folderId, subPath.Replace(Path.DirectorySeparatorChar, '/'));
        }
    }
}
