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
        public string Label { get; }
        public string Path { get; }

        private FolderSyncState _syncState;
        public FolderSyncState SyncState
        {
            get { lock (this.syncRoot) { return this._syncState; } }
            set { lock (this.syncRoot) { this._syncState = value; } }
        }

        private HashSet<string> syncingPaths { get; set; }

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


        public Folder(string folderId, string label, string path, FolderSyncState syncState, FolderStatus status)
        {
            this.FolderId = folderId;
            this.Label = String.IsNullOrWhiteSpace(label) ? folderId : label;
            this.Path = path;
            this.SyncState = syncState;
            this.syncingPaths = new HashSet<string>();
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

            lock (this.syncRoot)
            {
                return this.FolderId == other.FolderId &&
                    this.Label == other.Label &&
                    this.Path == other.Path &&
                    this.SyncState == other.SyncState &&
                    this.Status == other.Status &&
                    this.FolderErrors.SequenceEqual(other.FolderErrors) &&
                    this.syncingPaths.SetEquals(other.syncingPaths);
            }
        }

        public override int GetHashCode()
        {
            unchecked
            {
                lock (this.syncRoot)
                {
                    int hash = 17;
                    hash = hash * 23 + this.FolderId.GetHashCode();
                    hash = hash * 23 + this.Label.GetHashCode();
                    hash = hash * 23 + this.SyncState.GetHashCode();
                    hash = hash * 23 + this.Status.GetHashCode();
                    hash = hash * 23 + this.syncingPaths.GetHashCode();
                    foreach (var folderError in this.FolderErrors)
                    {
                        hash = hash * 23 + folderError.GetHashCode();
                    }
                    foreach (var syncingPath in this.syncingPaths)
                    {
                        hash = hash * 23 + syncingPath.GetHashCode();
                    }
                    return hash;
                }
            }
        }
    }
}
