using Stylet;
using SyncTrayzor.SyncThing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.NotifyIcon
{
    public class NotifyIconViewModel : PropertyChangedBase
    {
        private readonly ISyncThingManager syncThingManager;

        public bool Visible { get; set; }
        public bool MainWindowVisible { get; set; }

        public event EventHandler WindowOpenRequested;
        public event EventHandler WindowCloseRequested;
        public event EventHandler ExitRequested;

        public SyncThingState SyncThingState { get; set; }

        public bool SyncThingStarted
        {
            get { return this.SyncThingState != SyncThingState.Stopped; }
        }

        public bool SyncThingSyncing { get; private set; }

        public NotifyIconViewModel(ISyncThingManager syncThingManager)
        {
            this.syncThingManager = syncThingManager;

            this.syncThingManager.StateChanged += (o, e) => this.SyncThingState = e.NewState;
            this.SyncThingState = this.syncThingManager.State;

            this.syncThingManager.FolderSyncStateChanged += (o, e) =>
            {
                this.SyncThingSyncing = this.syncThingManager.Folders.Values.Any(x => x.SyncState == FolderSyncState.Syncing);
            };
        }

        public void DoubleClick()
        {
            this.OnWindowOpenRequested();
        }

        public void Restore()
        {
            this.OnWindowOpenRequested();
        }

        public void Minimize()
        {
            this.OnWindowCloseRequested();
        }

        public bool CanStart
        {
            get { return this.SyncThingState == SyncThingState.Stopped; }
        }
        public void Start()
        {
            this.syncThingManager.Start();
        }

        public bool CanStop
        {
            get { return this.SyncThingState == SyncThingState.Running; }
        }
        public void Stop()
        {
            this.syncThingManager.StopAsync();
        }

        public void Exit()
        {
            this.OnExitRequested();
        }

        private void OnWindowOpenRequested()
        {
            var handler = this.WindowOpenRequested;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void OnWindowCloseRequested()
        {
            var handler = this.WindowCloseRequested;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void OnExitRequested()
        {
            var handler = this.ExitRequested;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
    }
}
