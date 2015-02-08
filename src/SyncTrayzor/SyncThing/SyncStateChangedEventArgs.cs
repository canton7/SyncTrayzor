using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public class SyncStateChangedEventArgs : EventArgs
    {
        public string FolderId { get; private set; }
        public FolderSyncState PrevSyncState { get; private set; }
        public FolderSyncState SyncState { get; private set; }

        public SyncStateChangedEventArgs(string folderId, FolderSyncState prevSyncState, FolderSyncState syncState)
        {
            this.FolderId = folderId;
            this.PrevSyncState = prevSyncState;
            this.SyncState = syncState;
        }
    }
}
