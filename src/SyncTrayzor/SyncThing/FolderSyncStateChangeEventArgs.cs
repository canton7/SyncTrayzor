using System;

namespace SyncTrayzor.SyncThing
{
    public class FolderSyncStateChangeEventArgs : EventArgs
    {
        public Folder Folder { get; }
        public FolderSyncState PrevSyncState { get; }
        public FolderSyncState SyncState { get; }

        public FolderSyncStateChangeEventArgs(Folder folder, FolderSyncState prevSyncState, FolderSyncState syncState)
        {
            this.Folder = folder;
            this.PrevSyncState = prevSyncState;
            this.SyncState = syncState;
        }
    }
}
