using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public class FolderSyncStateChangeEventArgs : EventArgs
    {
        public Folder Folder { get; private set; }
        public FolderSyncState PrevSyncState { get; private set; }
        public FolderSyncState SyncState { get; private set; }

        public FolderSyncStateChangeEventArgs(Folder folder, FolderSyncState prevSyncState, FolderSyncState syncState)
        {
            this.Folder = folder;
            this.PrevSyncState = prevSyncState;
            this.SyncState = syncState;
        }
    }
}
