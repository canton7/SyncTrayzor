using Stylet;
using SyncTrayzor.Pages;
using SyncTrayzor.Services;
using SyncTrayzor.SyncThing;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SyncTrayzor.NotifyIcon
{
    public class NotifyIconViewModel : PropertyChangedBase
    {
        private readonly IWindowManager windowManager;
        private readonly ISyncThingManager syncThingManager;
        private readonly Func<SettingsViewModel> settingsViewModelFactory;
        private readonly IProcessStartProvider processStartProvider;

        public bool Visible { get; set; }
        public bool MainWindowVisible { get; set; }
        public BindableCollection<FolderViewModel> Folders { get; private set; }

        public event EventHandler WindowOpenRequested;
        public event EventHandler WindowCloseRequested;
        public event EventHandler ExitRequested;

        public SyncThingState SyncThingState { get; set; }

        public bool SyncThingStarted
        {
            get { return this.SyncThingState == SyncThingState.Running; }
        }

        public bool SyncThingSyncing { get; private set; }

        public NotifyIconViewModel(
            IWindowManager windowManager,
            ISyncThingManager syncThingManager,
            Func<SettingsViewModel> settingsViewModelFactory,
            IProcessStartProvider processStartProvider)
        {
            this.windowManager = windowManager;
            this.syncThingManager = syncThingManager;
            this.settingsViewModelFactory = settingsViewModelFactory;
            this.processStartProvider = processStartProvider;

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

            this.syncThingManager.DataLoaded += (o, e) =>
            {
                this.Folders = new BindableCollection<FolderViewModel>(this.syncThingManager.Folders.FetchAll()
                    .Select(x => new FolderViewModel(x, this.processStartProvider))
                    .OrderBy(x => x.FolderId));
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
        public async void Start()
        {
            await this.syncThingManager.StartWithErrorDialogAsync(this.windowManager);
        }

        public bool CanStop
        {
            get { return this.SyncThingState == SyncThingState.Running; }
        }
        public void Stop()
        {
            this.syncThingManager.StopAsync();
        }

        public bool CanRestart
        {
            get { return this.SyncThingState == SyncThingState.Running; }
        }
        public void Restart()
        {
            this.syncThingManager.RestartAsync();
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

    // Slightly hacky, as we can't use s:Action in a style setter...
    public class FolderViewModel : ICommand
    {
        private readonly Folder folder;
        private readonly IProcessStartProvider processStartProvider;

        public string FolderId { get { return this.folder.FolderId; } }

        public FolderViewModel(Folder folder, IProcessStartProvider processStartProvider)
        {
            this.folder = folder;
            this.processStartProvider = processStartProvider;
        }

        public event EventHandler CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object parameter) { return true; }

        public void Execute(object parameter)
        {
            this.processStartProvider.StartDetached("explorer.exe", this.folder.Path);
        }
    }
}
