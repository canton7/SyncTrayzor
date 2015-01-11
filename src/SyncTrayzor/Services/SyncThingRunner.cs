using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services
{
    public enum SyncThingState {  Started, Stopped }

    public class SyncThingStateChangedEventArgs : EventArgs
    {
        public SyncThingState State { get; private set; }

        public SyncThingStateChangedEventArgs(SyncThingState state)
        {
            this.State = state;
        }
    }

    public interface ISyncThingRunner
    {
        string ExecutablePath { get; set; }
        IObservable<string> LogMessages { get; }
        SyncThingState State { get; }

        event EventHandler<SyncThingStateChangedEventArgs> StateChanged;

        void Start();
        void Stop();
    }

    public class SyncThingRunner : ISyncThingRunner
    {
        public string ExecutablePath { get; set; }
        public SyncThingState State { get; private set; }
        public event EventHandler<SyncThingStateChangedEventArgs> StateChanged;
        public IObservable<string> LogMessages { get; private set; }

        public void Start()
        {

        }

        public void Stop()
        {

        }

        private void SetState(SyncThingState state)
        {
            this.State = state;
            var handler = this.StateChanged;
            if (handler != null)
                handler(this, new SyncThingStateChangedEventArgs(state));
        }
    }
}
