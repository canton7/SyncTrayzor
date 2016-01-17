using System;

namespace SyncTrayzor.Syncthing
{
    public class SyncthingStateChangedEventArgs : EventArgs
    {
        public SyncthingState OldState { get; }
        public SyncthingState NewState { get; }

        public SyncthingStateChangedEventArgs(SyncthingState oldState, SyncthingState newState)
        {
            this.OldState = oldState;
            this.NewState = newState;
        }
    }
}
