using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    // May contain uploading / downloading / etc
    public enum SyncState
    {
        Syncing,
        Idle,
    }

    public class SyncStateChangedEventArgs : EventArgs
    {
        public SyncState PrevSyncState { get; private set; }
        public SyncState SyncState { get; private set; }

        public SyncStateChangedEventArgs(SyncState prevSyncState, SyncState syncState)
        {
            this.PrevSyncState = prevSyncState;
            this.SyncState = syncState;
        }
    }
}
