using SyncTrayzor.SyncThing.ApiClient;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SyncTrayzor.SyncThing
{
    public enum FolderSyncState
    {
        Syncing,
        Idle,
    }

    public class FolderIgnores
    {
        public IReadOnlyList<string> IgnorePatterns { get; }
        public IReadOnlyList<Regex> IncludeRegex { get; }
        public IReadOnlyList<Regex> ExcludeRegex { get; }

        public FolderIgnores(List<string> ignores, List<string> patterns)
        {
            this.IgnorePatterns = ignores;
            var includeRegex = new List<Regex>();
            var excludeRegex = new List<Regex>();

            foreach (var pattern in patterns)
            {
                if (pattern.StartsWith("(?exclude)"))
                    excludeRegex.Add(new Regex(pattern.Substring("(?exclude)".Length)));
                else
                    includeRegex.Add(new Regex(pattern));
            }

            this.IncludeRegex = includeRegex.AsReadOnly();
            this.ExcludeRegex = excludeRegex.AsReadOnly();
        }
    }

    public class Folder
    {
        public string FolderId { get; }
        public string Path { get; }

        private readonly object syncStateLock = new object();
        private FolderSyncState _syncState;
        public FolderSyncState SyncState
        {
            get { lock (this.syncStateLock) { return this._syncState; } }
            set { lock (this.syncStateLock) { this._syncState = value; } }
        }

        private readonly object syncingPathsLock = new object();
        private HashSet<string> syncingPaths { get; set; }

        private readonly object ignoresLock = new object();
        private FolderIgnores _ignores;
        public FolderIgnores Ignores
        {
            get { lock (this.ignoresLock) { return this._ignores; } }
            set { lock (this.ignoresLock) { this._ignores = value; } }
        }

        private readonly object statusLock = new object();
        private FolderStatus _status;
        public FolderStatus Status
        {
            get {  lock(this.statusLock) { return this._status; } }
            set {  lock(this.statusLock) { this._status = value; } }
        }

        public Folder(string folderId, string path, FolderSyncState syncState, FolderIgnores ignores, FolderStatus status)
        {
            this.FolderId = folderId;
            this.Path = path;
            this.SyncState = syncState;
            this.syncingPaths = new HashSet<string>();
            this._ignores = ignores;
            this._status = status;
        }

        public bool IsSyncingPath(string path)
        {
            lock (this.syncingPathsLock)
            {
                return this.syncingPaths.Contains(path);
            }
        }

        public void AddSyncingPath(string path)
        {
            lock (this.syncingPathsLock)
            {
                this.syncingPaths.Add(path);
            }
        }

        public void RemoveSyncingPath(string path)
        {
            lock (this.syncingPathsLock)
            {
                this.syncingPaths.Remove(path);
            }
        }
    }
}
