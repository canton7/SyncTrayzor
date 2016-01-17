using System;

namespace SyncTrayzor.Syncthing
{
    public class SyncStateChangedEventArgs : EventArgs
    {
        public string FolderId { get; }
        public FolderSyncState PrevSyncState { get; }
        public FolderSyncState SyncState { get; }

        public SyncStateChangedEventArgs(string folderId, FolderSyncState prevSyncState, FolderSyncState syncState)
        {
            this.FolderId = folderId;
            this.PrevSyncState = prevSyncState;
            this.SyncState = syncState;
        }
    }
}
