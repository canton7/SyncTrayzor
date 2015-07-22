using System;

namespace SyncTrayzor.SyncThing
{
    public class SyncThingStateChangedEventArgs : EventArgs
    {
        public SyncThingState OldState { get; }
        public SyncThingState NewState { get; }

        public SyncThingStateChangedEventArgs(SyncThingState oldState, SyncThingState newState)
        {
            this.OldState = oldState;
            this.NewState = newState;
        }
    }
}
