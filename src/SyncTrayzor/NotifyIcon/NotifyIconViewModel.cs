using Stylet;
using SyncTrayzor.Pages;
using SyncTrayzor.SyncThing;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.NotifyIcon
{
    public class NotifyIconViewModel : PropertyChangedBase
    {
        private readonly IWindowManager windowManager;
        private readonly ISyncThingManager syncThingManager;
        private readonly Func<SettingsViewModel> settingsViewModelFactory;

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

        public NotifyIconViewModel(
            IWindowManager windowManager,
            ISyncThingManager syncThingManager,
            Func<SettingsViewModel> settingsViewModelFactory)
        {
            this.windowManager = windowManager;
            this.syncThingManager = syncThingManager;
            this.settingsViewModelFactory = settingsViewModelFactory;

            this.syncThingManager.StateChanged += (o, e) =>
            {
                this.SyncThingState = e.NewState;
                if (e.NewState != SyncThingState.Running)
                    this.SyncThingSyncing = false; // Just make sure we reset this...
            };
            this.SyncThingState = this.syncThingManager.State;

            this.syncThingManager.TotalConnectionStatsChanged += (o, e) =>
            {
                var stats = e.TotalConnectionStats;
                this.SyncThingSyncing = stats.InBytesPerSecond > 0 || stats.OutBytesPerSecond > 0;
            };
        }

        public void DoubleClick()
        {
            this.OnWindowOpenRequested();
        }

        public void ShowSettings()
        {
            var vm = this.settingsViewModelFactory();
            this.windowManager.ShowDialog(vm);
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
            this.syncThingManager.StartWithErrorDialog(this.windowManager);
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
