using Stylet;
using SyncTrayzor.Localization;
using SyncTrayzor.NotifyIcon;
using SyncTrayzor.Services;
using SyncTrayzor.Services.Config;
using SyncTrayzor.SyncThing;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SyncTrayzor.Pages
{
    public class ShellViewModel : Screen
    {
        private readonly IWindowManager windowManager;
        private readonly ISyncThingManager syncThingManager;
        private readonly IApplicationState application;
        private readonly IConfigurationProvider configurationProvider;
        private readonly Func<SettingsViewModel> settingsViewModelFactory;
        private readonly Func<AboutViewModel> aboutViewModelFactory;

        public bool WindowActivated { get; set; }
        public bool ShowConsole { get; set; }
        public ConsoleViewModel Console { get; private set; }
        public ViewerViewModel Viewer { get; private set; }

        public SyncThingState SyncThingState { get; private set; }

        public ShellViewModel(
            IWindowManager windowManager,
            ISyncThingManager syncThingManager,
            IApplicationState application,
            IConfigurationProvider configurationProvider,
            ConsoleViewModel console,
            ViewerViewModel viewer,
            Func<SettingsViewModel> settingsViewModelFactory,
            Func<AboutViewModel> aboutViewModelFactory)
        {
            this.windowManager = windowManager;
            this.syncThingManager = syncThingManager;
            this.application = application;
            this.configurationProvider = configurationProvider;
            this.Console = console;
            this.Viewer = viewer;
            this.settingsViewModelFactory = settingsViewModelFactory;
            this.aboutViewModelFactory = aboutViewModelFactory;

            this.Console.ConductWith(this);
            this.Viewer.ConductWith(this);

            this.syncThingManager.StateChanged += (o, e) => this.SyncThingState = e.NewState;
            this.syncThingManager.ProcessExitedWithError += (o, e) => this.ShowExitedWithError();

            this.ShowConsole = this.configurationProvider.Load().ShowSyncthingConsole;
            this.Bind(s => s.ShowConsole, (o, e) => this.SetConsoleVisible(e.NewValue));
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
            get { return this.SyncThingState == SyncThing.SyncThingState.Running; }
        }
        public void Restart()
        {
            this.syncThingManager.RestartAsync();
        }

        public bool CanKill
        {
            get { return this.SyncThingState != SyncThingState.Stopped; }
        }
        public void Kill()
        {
            this.syncThingManager.Kill();
        }

        public bool CanRefreshBrowser
        {
            get { return this.SyncThingState == SyncThingState.Running; }
        }
        public void RefreshBrowser()
        {
            this.Viewer.RefreshBrowser();
        }

        public bool CanOpenBrowser
        {
            get { return this.SyncThingState == SyncThingState.Running; }
        }
        public void OpenBrowser()
        {
            Process.Start(this.syncThingManager.Address.NormalizeZeroHost().ToString());
        }

        public void KillAllSyncthingProcesses()
        {
            if (this.windowManager.ShowMessageBox(
                    Localizer.Translate("Dialog_ConfirmKillAllProcesses_Message"),
                    Localizer.Translate("Dialog_ConfirmKillAllProcesses_Title"),
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                this.syncThingManager.KillAllSyncthingProcesses();
        }

        public void ShowSettings()
        {
            var vm = this.settingsViewModelFactory();
            this.windowManager.ShowDialog(vm);
        }

        public void ShowAbout()
        {
            var vm = this.aboutViewModelFactory();
            this.windowManager.ShowDialog(vm);
        }

        public void SetConsoleVisible(bool visible)
        {
            this.configurationProvider.AtomicLoadAndSave(configuration => configuration.ShowSyncthingConsole = visible);
        }

        public void ShowExitedWithError()
        {
            this.windowManager.ShowMessageBox(
                Localizer.Translate("Dialog_FailedToStartSyncthing_Message"),
                Localizer.Translate("Dialog_FailedToStartSyncthing_Title"),
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
            this.WindowActivated = true;
        }
    }
}
