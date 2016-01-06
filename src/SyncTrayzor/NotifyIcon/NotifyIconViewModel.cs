using Stylet;
using SyncTrayzor.Pages;
using SyncTrayzor.Pages.Settings;
using SyncTrayzor.Services;
using SyncTrayzor.SyncThing;
using SyncTrayzor.Utils;
using System;
using System.Linq;
using System.Windows.Input;

namespace SyncTrayzor.NotifyIcon
{
    public class NotifyIconViewModel : PropertyChangedBase
    {
        private readonly IWindowManager windowManager;
        private readonly ISyncThingManager syncThingManager;
        private readonly Func<SettingsViewModel> settingsViewModelFactory;
        private readonly IProcessStartProvider processStartProvider;
        private readonly IAlertsManager alertsManager;

        public bool Visible { get; set; }
        public bool MainWindowVisible { get; set; }
        public BindableCollection<FolderViewModel> Folders { get; private set; }
        public FileTransfersTrayViewModel FileTransfersViewModel { get; private set; }

        public event EventHandler WindowOpenRequested;
        public event EventHandler WindowCloseRequested;
        public event EventHandler ExitRequested;

        public SyncThingState SyncThingState { get; set; }

        public bool SyncThingAlert => this.alertsManager.AnyAlerts;

        public bool SyncThingStarted => this.SyncThingState == SyncThingState.Running;

        public bool SyncThingSyncing { get; private set; }

        public NotifyIconViewModel(
            IWindowManager windowManager,
            ISyncThingManager syncThingManager,
            Func<SettingsViewModel> settingsViewModelFactory,
            IProcessStartProvider processStartProvider,
            IAlertsManager alertsManager,
            FileTransfersTrayViewModel fileTransfersViewModel)
        {
            this.windowManager = windowManager;
            this.syncThingManager = syncThingManager;
            this.settingsViewModelFactory = settingsViewModelFactory;
            this.processStartProvider = processStartProvider;
            this.alertsManager = alertsManager;
            this.FileTransfersViewModel = fileTransfersViewModel;

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

            this.alertsManager.AlertsStateChanged += (o, e) => this.NotifyOfPropertyChange(nameof(this.SyncThingAlert));
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

        public bool CanStart => this.SyncThingState == SyncThingState.Stopped;
        public async void Start()
        {
            await this.syncThingManager.StartWithErrorDialogAsync(this.windowManager);
        }

        public bool CanStop => this.SyncThingState == SyncThingState.Running;
        public void Stop()
        {
            this.syncThingManager.StopAsync();
        }

        public bool CanRestart => this.SyncThingState == SyncThingState.Running;
        public void Restart()
        {
            this.syncThingManager.RestartAsync();
        }

        public void Exit()
        {
            this.OnExitRequested();
        }

        private void OnWindowOpenRequested() => this.WindowOpenRequested?.Invoke(this, EventArgs.Empty);

        private void OnWindowCloseRequested() => this.WindowCloseRequested?.Invoke(this, EventArgs.Empty);

        private void OnExitRequested() => this.ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    // Slightly hacky, as we can't use s:Action in a style setter...
    public class FolderViewModel : ICommand
    {
        private readonly Folder folder;
        private readonly IProcessStartProvider processStartProvider;

        public string FolderId => this.folder.FolderId;

        public FolderViewModel(Folder folder, IProcessStartProvider processStartProvider)
        {
            this.folder = folder;
            this.processStartProvider = processStartProvider;
        }

        public event EventHandler CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            this.processStartProvider.StartDetached("explorer.exe", this.folder.Path);
        }
    }
}
