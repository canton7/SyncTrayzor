using System;

namespace SyncTrayzor.SyncThing
{
    public class FolderSyncStateChangedEventArgs : EventArgs
    {
        public string FolderId { get; }
        public FolderSyncState PrevSyncState { get; }
        public FolderSyncState SyncState { get; }

        public FolderSyncStateChangedEventArgs(string folderId, FolderSyncState prevSyncState, FolderSyncState syncState)
        {
            this.FolderId = folderId;
            this.PrevSyncState = prevSyncState;
            this.SyncState = syncState;
        }
    }
}
