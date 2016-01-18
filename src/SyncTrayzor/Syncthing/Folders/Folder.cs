using SyncTrayzor.Syncthing.ApiClient;
using System.Collections.Generic;

namespace SyncTrayzor.Syncthing.Folders
{
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
