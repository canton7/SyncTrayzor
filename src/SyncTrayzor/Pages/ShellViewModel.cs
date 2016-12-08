using Stylet;
using SyncTrayzor.Pages.BarAlerts;
using SyncTrayzor.Pages.ConflictResolution;
using SyncTrayzor.Pages.Settings;
using SyncTrayzor.Properties;
using SyncTrayzor.Services;
using SyncTrayzor.Services.Config;
using SyncTrayzor.Syncthing;
using SyncTrayzor.Utils;
using System;
using System.Reactive.Subjects;
using System.Windows;

namespace SyncTrayzor.Pages
{
    public class ShellViewModel : Screen, IDisposable
    {
        private readonly IWindowManager windowManager;
        private readonly ISyncthingManager syncthingManager;
        private readonly IApplicationState application;
        private readonly IConfigurationProvider configurationProvider;
        private readonly Func<SettingsViewModel> settingsViewModelFactory;
        private readonly Func<AboutViewModel> aboutViewModelFactory;
        private readonly Func<ConflictResolutionViewModel> confictResolutionViewModelFactory;
        private readonly IProcessStartProvider processStartProvider;

        public bool ShowConsole { get; set; }
        public double ConsoleHeight { get; set; }
        public WindowPlacement Placement { get; set; }

        private readonly Subject<bool> _activateObservable = new Subject<bool>();
        public IObservable<bool> ActivateObservable => this._activateObservable;
        public ConsoleViewModel Console { get; }
        public ViewerViewModel Viewer { get; }
        public BarAlertsViewModel BarAlerts { get; }

        public SyncthingState SyncthingState { get; private set; }

        public ShellViewModel(
            IWindowManager windowManager,
            ISyncthingManager syncthingManager,
            IApplicationState application,
            IConfigurationProvider configurationProvider,
            ConsoleViewModel console,
            ViewerViewModel viewer,
            BarAlertsViewModel barAlerts,
            Func<SettingsViewModel> settingsViewModelFactory,
            Func<AboutViewModel> aboutViewModelFactory,
            Func<ConflictResolutionViewModel> confictResolutionViewModelFactory,
            IProcessStartProvider processStartProvider)
        {
            this.windowManager = windowManager;
            this.syncthingManager = syncthingManager;
            this.application = application;
            this.configurationProvider = configurationProvider;
            this.Console = console;
            this.Viewer = viewer;
            this.BarAlerts = barAlerts;
            this.settingsViewModelFactory = settingsViewModelFactory;
            this.aboutViewModelFactory = aboutViewModelFactory;
            this.confictResolutionViewModelFactory = confictResolutionViewModelFactory;
            this.processStartProvider = processStartProvider;

            var configuration = this.configurationProvider.Load();

            this.Console.ConductWith(this);
            this.Viewer.ConductWith(this);
            this.BarAlerts.ConductWith(this);

            this.syncthingManager.StateChanged += (o, e) => this.SyncthingState = e.NewState;
            this.syncthingManager.ProcessExitedWithError += (o, e) => this.ShowExitedWithError();

            this.ConsoleHeight = configuration.SyncthingConsoleHeight;
            this.Bind(s => s.ConsoleHeight, (o, e) => this.configurationProvider.AtomicLoadAndSave(c => c.SyncthingConsoleHeight = e.NewValue));

            this.ShowConsole = configuration.SyncthingConsoleHeight > 0;
            this.Bind(s => s.ShowConsole, (o, e) =>
            {
                this.ConsoleHeight = e.NewValue ? Configuration.DefaultSyncthingConsoleHeight : 0.0;
            });

            this.Placement = configuration.WindowPlacement;
            this.Bind(s => s.Placement, (o, e) => this.configurationProvider.AtomicLoadAndSave(c => c.WindowPlacement = e.NewValue));
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

        public bool CanRefreshBrowser => this.SyncthingState == SyncthingState.Running;
        public void RefreshBrowser()
        {
            this.Viewer.RefreshBrowserNukeCache();
        }

        public bool CanOpenBrowser => this.SyncthingState == SyncthingState.Running;
        public void OpenBrowser()
        {
            this.processStartProvider.StartDetached(this.syncthingManager.Address.NormalizeZeroHost().ToString());
        }

        public void KillAllSyncthingProcesses()
        {
            if (this.windowManager.ShowMessageBox(
                    Resources.Dialog_ConfirmKillAllProcesses_Message,
                    Resources.Dialog_ConfirmKillAllProcesses_Title,
                    MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                this.syncthingManager.KillAllSyncthingProcesses();
        }

        public void ShowSettings()
        {
            var vm = this.settingsViewModelFactory();
            this.windowManager.ShowDialog(vm);
        }

        public void ShowConflictResolver()
        {
            var vm = this.confictResolutionViewModelFactory();
            this.windowManager.ShowDialog(vm);
        }

        public bool CanZoomBrowser => this.SyncthingState == SyncthingState.Running;

        public void BrowserZoomIn()
        {
            this.Viewer.ZoomIn();
        }

        public void BrowserZoomOut()
        {
            this.Viewer.ZoomOut();
        }

        public void BrowserZoomReset()
        {
            this.Viewer.ZoomReset();
        }

        public void ShowAbout()
        {
            var vm = this.aboutViewModelFactory();
            this.windowManager.ShowDialog(vm);
        }

        public void ShowExitedWithError()
        {
            this.windowManager.ShowMessageBox(
                Resources.Dialog_FailedToStartSyncthing_Message,
                Resources.Dialog_FailedToStartSyncthing_Title,
                icon: MessageBoxImage.Error);
        }

        public void CloseToTray()
        {
            this.RequestClose();
        }

        public void Shutdown()
        {
            this.application.Shutdown();
        }

        public void EnsureInForeground()
        {
            if (!this.application.HasMainWindow)
                this.windowManager.ShowWindow(this);

            this._activateObservable.OnNext(true);
        }

        public void Dispose()
        {
            this.Viewer.Dispose();
            this.Console.Dispose();
        }
    }
}
