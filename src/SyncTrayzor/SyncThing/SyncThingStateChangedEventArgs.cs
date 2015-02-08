using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public class SyncThingStateChangedEventArgs : EventArgs
    {
        public SyncThingState OldState { get; private set; }
        public SyncThingState NewState { get; private set; }

        public SyncThingStateChangedEventArgs(SyncThingState oldState, SyncThingState newState)
        {
            this.OldState = oldState;
            this.NewState = newState;
        }
    }
}
