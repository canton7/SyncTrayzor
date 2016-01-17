using Stylet;
using SyncTrayzor.Pages;
using SyncTrayzor.Pages.Settings;
using SyncTrayzor.Services;
using SyncTrayzor.Syncthing;
using SyncTrayzor.Utils;
using System;
using System.Linq;
using System.Windows.Input;

namespace SyncTrayzor.NotifyIcon
{
    public class NotifyIconViewModel : PropertyChangedBase, IDisposable
    {
        private readonly IWindowManager windowManager;
        private readonly ISyncthingManager syncthingManager;
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

        public SyncthingState SyncthingState { get; set; }

        public bool SyncthingAlert => this.alertsManager.AnyAlerts;

        public bool SyncthingStarted => this.SyncthingState == SyncthingState.Running;

        public bool SyncthingSyncing { get; private set; }

        public NotifyIconViewModel(
            IWindowManager windowManager,
            ISyncthingManager syncthingManager,
            Func<SettingsViewModel> settingsViewModelFactory,
            IProcessStartProvider processStartProvider,
            IAlertsManager alertsManager,
            FileTransfersTrayViewModel fileTransfersViewModel)
        {
            this.windowManager = windowManager;
            this.syncthingManager = syncthingManager;
            this.settingsViewModelFactory = settingsViewModelFactory;
            this.processStartProvider = processStartProvider;
            this.alertsManager = alertsManager;
            this.FileTransfersViewModel = fileTransfersViewModel;

            this.syncthingManager.StateChanged += this.StateChanged;
            this.SyncthingState = this.syncthingManager.State;

            this.syncthingManager.TotalConnectionStatsChanged += this.TotalConnectionStatsChanged;
            this.syncthingManager.DataLoaded += this.DataLoaded;

            this.alertsManager.AlertsStateChanged += this.AlertsStateChanged;
        }

        private void StateChanged(object sender, SyncthingStateChangedEventArgs e)
        {
            this.SyncthingState = e.NewState;
            if (e.NewState != SyncthingState.Running)
                this.SyncthingSyncing = false; // Just make sure we reset this..
        }

        private void TotalConnectionStatsChanged(object sender, ConnectionStatsChangedEventArgs e)
        {
            var stats = e.TotalConnectionStats;
            this.SyncthingSyncing = stats.InBytesPerSecond > 0 || stats.OutBytesPerSecond > 0;
        }

        private void DataLoaded(object sender, EventArgs e)
        {
            this.Folders = new BindableCollection<FolderViewModel>(this.syncthingManager.Folders.FetchAll()
                    .Select(x => new FolderViewModel(x, this.processStartProvider))
                    .OrderBy(x => x.FolderId));
        }

        private void AlertsStateChanged(object sender, EventArgs e)
        {
            this.NotifyOfPropertyChange(nameof(this.SyncthingAlert));
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

        public bool CanStart => this.SyncthingState == SyncthingState.Stopped;
        public async void Start()
        {
            await this.syncthingManager.StartWithErrorDialogAsync(this.windowManager);
        }

        public bool CanStop => this.SyncthingState == SyncthingState.Running;
        public void Stop()
        {
            this.syncthingManager.StopAsync();
        }

        public bool CanRestart => this.SyncthingState == SyncthingState.Running;
        public void Restart()
        {
            this.syncthingManager.RestartAsync();
        }

        public void Exit()
        {
            this.OnExitRequested();
        }

        private void OnWindowOpenRequested() => this.WindowOpenRequested?.Invoke(this, EventArgs.Empty);

        private void OnWindowCloseRequested() => this.WindowCloseRequested?.Invoke(this, EventArgs.Empty);

        private void OnExitRequested() => this.ExitRequested?.Invoke(this, EventArgs.Empty);

        public void Dispose()
        {
            this.syncthingManager.StateChanged -= this.StateChanged;

            this.syncthingManager.TotalConnectionStatsChanged -= this.TotalConnectionStatsChanged;
            this.syncthingManager.DataLoaded -= this.DataLoaded;

            this.alertsManager.AlertsStateChanged -= this.AlertsStateChanged;
        }
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
