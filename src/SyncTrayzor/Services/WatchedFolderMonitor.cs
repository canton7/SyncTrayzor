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
        TimeSpan BackoffInterval { get; set; }
        TimeSpan FolderExistenceCheckingInterval { get; set; }
    }

    public class WatchedFolderMonitor : IWatchedFolderMonitor
    {
        // Paths we don't alert Syncthing about
        private static readonly string ignoresFilePath = ".stignore";
        private static readonly string[] specialPaths = new[] { ".stversions", ".stfolder", "~syncthing~", ".syncthing." };
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

        public TimeSpan BackoffInterval { get; set; }
        public TimeSpan FolderExistenceCheckingInterval { get; set; }

        public WatchedFolderMonitor(ISyncThingManager syncThingManager)
        {
            this.syncThingManager = syncThingManager;
            this.syncThingManager.DataLoaded += (o, e) => this.Reset();
            this.syncThingManager.StateChanged += (o, e) => this.Reset();
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

            if (this.syncThingManager.State != SyncThingState.Running || !this.syncThingManager.IsDataLoaded)
                return;

            foreach (var folder in this.syncThingManager.FetchAllFolders())
            {
                if (!this._watchedFolders.Contains(folder.FolderId))
                    continue;

                var watcher = new DirectoryWatcher(folder.Path, this.BackoffInterval, this.FolderExistenceCheckingInterval);
                watcher.PreviewDirectoryChanged += (o, e) => e.Cancel = this.PreviewDirectoryChanged(folder, e.SubPath);
                watcher.DirectoryChanged += (o, e) => this.DirectoryChanged(folder, e.SubPath);

                this.directoryWatchers.Add(watcher);
            }
        }

        // Returns true to cancel
        private bool PreviewDirectoryChanged(Folder folder, string subPath)
        {
            // Is it a syncthing temp/special path?
            if (specialPaths.Any(x => subPath.StartsWith(x)))
                return true;

            if (subPath == ignoresFilePath)
            {
                // Extra: tell SyncThing to update its ignores list
                this.syncThingManager.ReloadIgnoresAsync(folder.FolderId);
                return true;
            }

            if (folder.SyncState == FolderSyncState.Syncing || folder.IsSyncingPath(subPath))
                return true;

            // Syncthing applies regex from the top down - if a parent is ignored, all of its children are by default
            var pathParts = subPath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            var cumulative = String.Empty;
            foreach (var pathPart in pathParts)
            {
                cumulative = Path.Combine(cumulative, pathPart);
                // If there's an include match on it, and not an exclude match, we ignore it
                if (folder.Ignores.IncludeRegex.Any(x => x.Match(cumulative).Success) && !folder.Ignores.ExcludeRegex.Any(x => x.Match(cumulative).Success))
                    return true;
            }

            return false;
        }

        private void DirectoryChanged(Folder folder, string subPath)
        {
            // If it's currently syncing, then don't refresh it
            if (folder.SyncState == FolderSyncState.Syncing)
                return;

            this.syncThingManager.ScanAsync(folder.FolderId, subPath.Replace(Path.DirectorySeparatorChar, '/'));
        }
    }
}
