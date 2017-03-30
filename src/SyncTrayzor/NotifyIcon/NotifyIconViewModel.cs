using Stylet;
using SyncTrayzor.Pages;
using SyncTrayzor.Pages.Settings;
using SyncTrayzor.Pages.Tray;
using SyncTrayzor.Services;
using SyncTrayzor.Services.Config;
using SyncTrayzor.Syncthing;
using SyncTrayzor.Syncthing.Folders;
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
        private readonly IConfigurationProvider configurationProvider;

        public bool Visible { get; set; }
        public bool MainWindowVisible { get; set; }
        public BindableCollection<FolderViewModel> Folders { get; private set; }
        public FileTransfersTrayViewModel FileTransfersViewModel { get; private set; }

        public event EventHandler WindowOpenRequested;
        public event EventHandler WindowCloseRequested;
        public event EventHandler ExitRequested;

        public SyncthingState SyncthingState { get; set; }

        public bool SyncthingDevicesPaused => this.alertsManager.PausedDeviceIdsFromMetering.Count > 0;

        public bool SyncthingWarning => this.alertsManager.AnyWarnings;

        public bool SyncthingStarted => this.SyncthingState == SyncthingState.Running;

        public bool SyncthingSyncing { get; private set; }

        private IconAnimationMode iconAnimationmode;

        public NotifyIconViewModel(
            IWindowManager windowManager,
            ISyncthingManager syncthingManager,
            Func<SettingsViewModel> settingsViewModelFactory,
            IProcessStartProvider processStartProvider,
            IAlertsManager alertsManager,
            FileTransfersTrayViewModel fileTransfersViewModel,
            IConfigurationProvider configurationProvider)
        {
            this.windowManager = windowManager;
            this.syncthingManager = syncthingManager;
            this.settingsViewModelFactory = settingsViewModelFactory;
            this.processStartProvider = processStartProvider;
            this.alertsManager = alertsManager;
            this.FileTransfersViewModel = fileTransfersViewModel;
            this.configurationProvider = configurationProvider;

            this.syncthingManager.StateChanged += this.StateChanged;
            this.SyncthingState = this.syncthingManager.State;

            this.syncthingManager.TotalConnectionStatsChanged += this.TotalConnectionStatsChanged;
            this.syncthingManager.Folders.FoldersChanged += this.FoldersChanged;
            this.syncthingManager.Folders.SyncStateChanged += this.FolderSyncStateChanged;


            this.alertsManager.AlertsStateChanged += this.AlertsStateChanged;

            this.configurationProvider.ConfigurationChanged += this.ConfigurationChanged;
            this.iconAnimationmode = this.configurationProvider.Load().IconAnimationMode;
        }

        private void StateChanged(object sender, SyncthingStateChangedEventArgs e)
        {
            this.SyncthingState = e.NewState;
            if (e.NewState != SyncthingState.Running)
                this.SyncthingSyncing = false; // Just make sure we reset this..
        }

        private void TotalConnectionStatsChanged(object sender, ConnectionStatsChangedEventArgs e)
        {
            if (this.iconAnimationmode == IconAnimationMode.DataTransferring)
            {
                var stats = e.TotalConnectionStats;
                this.SyncthingSyncing = stats.InBytesPerSecond > 0 || stats.OutBytesPerSecond > 0;
            }
        }

        private void FoldersChanged(object sender, EventArgs e)
        {
            this.Folders = new BindableCollection<FolderViewModel>(this.syncthingManager.Folders.FetchAll()
                    .Select(x => new FolderViewModel(x, this.processStartProvider))
                    .OrderBy(x => x.FolderLabel));
        }

        private void FolderSyncStateChanged(object sender, FolderSyncStateChangedEventArgs e)
        {
            if (this.iconAnimationmode == IconAnimationMode.Syncing)
            {
                var anySyncing = this.syncthingManager.Folders.FetchAll().Any(x => x.SyncState == FolderSyncState.Syncing);
                this.SyncthingSyncing = anySyncing;
            }
        }

        private void AlertsStateChanged(object sender, EventArgs e)
        {
            this.NotifyOfPropertyChange(nameof(this.SyncthingDevicesPaused));
            this.NotifyOfPropertyChange(nameof(this.SyncthingWarning));
        }

        private void ConfigurationChanged(object sender, ConfigurationChangedEventArgs e)
        {
            this.iconAnimationmode = e.NewConfiguration.IconAnimationMode;
            // Reset, just in case
            this.SyncthingSyncing = false;
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
            this.syncthingManager.Folders.SyncStateChanged -= this.FolderSyncStateChanged;
            this.syncthingManager.Folders.FoldersChanged -= this.FoldersChanged;

            this.alertsManager.AlertsStateChanged -= this.AlertsStateChanged;

            this.configurationProvider.ConfigurationChanged -= this.ConfigurationChanged;
        }
    }

    // Slightly hacky, as we can't use s:Action in a style setter...
    public class FolderViewModel : ICommand
    {
        private readonly Folder folder;
        private readonly IProcessStartProvider processStartProvider;

        public string FolderLabel => this.folder.Label;

        public FolderViewModel(Folder folder, IProcessStartProvider processStartProvider)
        {
            this.folder = folder;
            this.processStartProvider = processStartProvider;
        }

        public event EventHandler CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object parameter) => true;

        public void Execute(object parameter)
        {
            this.processStartProvider.ShowFolderInExplorer(this.folder.Path);
        }
    }
}
