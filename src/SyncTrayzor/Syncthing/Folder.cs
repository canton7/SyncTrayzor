using SyncTrayzor.Syncthing.ApiClient;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SyncTrayzor.Syncthing
{
    public enum FolderSyncState
    {
        Syncing,
        Scanning,
        Idle,
        Error,
    }

    public class FolderIgnores
    {
        public IReadOnlyList<string> IgnorePatterns { get; }
        public IReadOnlyList<Regex> IncludeRegex { get; }
        public IReadOnlyList<Regex> ExcludeRegex { get; }

        public FolderIgnores()
        {
            this.IgnorePatterns = EmptyList<string>.Instance;
            this.IncludeRegex = EmptyList<Regex>.Instance;
            this.ExcludeRegex = EmptyList<Regex>.Instance;
        }

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

        private static class EmptyList<T>
        {
            public static IReadOnlyList<T> Instance = new List<T>().AsReadOnly();
        }
    }

    public class FolderError
    {
        public string Error { get; }
        public string Path { get; }

        public FolderError(string error, string path)
        {
            this.Error = error;
            this.Path = path;
        }
    }

    public class Folder
    {
        private readonly object syncRoot = new object();

        public string FolderId { get; }
        public string Path { get; }

        private FolderSyncState _syncState;
        public FolderSyncState SyncState
        {
            get { lock (this.syncRoot) { return this._syncState; } }
            set { lock (this.syncRoot) { this._syncState = value; } }
        }

        private HashSet<string> syncingPaths { get; set; }

        private FolderIgnores _ignores;
        public FolderIgnores Ignores
        {
            get { lock (this.syncRoot) { return this._ignores; } }
            set { lock (this.syncRoot) { this._ignores = value; } }
        }

        private FolderStatus _status;
        public FolderStatus Status
        {
            get {  lock(this.syncRoot) { return this._status; } }
            set {  lock(this.syncRoot) { this._status = value; } }
        }

        private readonly List<FolderError> _folderErrorsList = new List<FolderError>();
        private readonly IReadOnlyList<FolderError> _folderErrors;
        public IReadOnlyList<FolderError> FolderErrors
        {
            get {  lock(this.syncRoot) { return this._folderErrors; } }
        }


        public Folder(string folderId, string path, FolderSyncState syncState, FolderIgnores ignores, FolderStatus status)
        {
            this.FolderId = folderId;
            this.Path = path;
            this.SyncState = syncState;
            this.syncingPaths = new HashSet<string>();
            this._ignores = ignores;
            this._status = status;
            this._folderErrors = this._folderErrorsList.AsReadOnly();
        }

        public bool IsSyncingPath(string path)
        {
            lock (this.syncRoot)
            {
                return this.syncingPaths.Contains(path);
            }
        }

        public void AddSyncingPath(string path)
        {
            lock (this.syncRoot)
            {
                this.syncingPaths.Add(path);
            }
        }

        public void RemoveSyncingPath(string path)
        {
            lock (this.syncRoot)
            {
                this.syncingPaths.Remove(path);
            }
        }

        public void SetFolderErrors(IEnumerable<FolderError> folderErrors)
        {
            lock (this.syncRoot)
            {
                this._folderErrorsList.Clear();
                this._folderErrorsList.AddRange(folderErrors);
            }
        }

        public void ClearFolderErrors()
        {
            lock (this.syncRoot)
            {
                this._folderErrorsList.Clear();
            }
        }
    }
}
