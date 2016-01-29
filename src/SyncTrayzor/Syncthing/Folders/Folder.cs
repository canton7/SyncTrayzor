using SyncTrayzor.Syncthing.ApiClient;
using System.Collections.Generic;
using System;
using System.Linq;

namespace SyncTrayzor.Syncthing.Folders
{
    public class Folder : IEquatable<Folder>
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
            get { lock(this.syncRoot) { return this._status; } }
            set { lock(this.syncRoot) { this._status = value; } }
        }

        private IReadOnlyList<FolderError> _folderErrors;
        public IReadOnlyList<FolderError> FolderErrors
        {
            get { lock(this.syncRoot) { return this._folderErrors; } }
            private set { lock(this.syncRoot) { this._folderErrors = value; } }
        }


        public Folder(string folderId, string path, FolderSyncState syncState, FolderIgnores ignores, FolderStatus status)
        {
            this.FolderId = folderId;
            this.Path = path;
            this.SyncState = syncState;
            this.syncingPaths = new HashSet<string>();
            this._ignores = ignores;
            this._status = status;
            this.FolderErrors = new List<FolderError>().AsReadOnly();
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
            this.FolderErrors = folderErrors.ToList().AsReadOnly();
        }

        public void ClearFolderErrors()
        {
            this.FolderErrors = new List<FolderError>().AsReadOnly();
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Folder);
        }

        public bool Equals(Folder other)
        {
            if (Object.ReferenceEquals(this, other))
                return true;
            if (Object.ReferenceEquals(other, null))
                return false;

            return this.FolderId == other.FolderId;
        }

        public override int GetHashCode()
        {
            return this.FolderId.GetHashCode();
        }
    }
}
