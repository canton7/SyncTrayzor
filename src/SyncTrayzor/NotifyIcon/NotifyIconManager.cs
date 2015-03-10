using Hardcodet.Wpf.TaskbarNotification;
using Stylet;
using SyncTrayzor.Services;
using SyncTrayzor.SyncThing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SyncTrayzor.NotifyIcon
{
    public interface INotifyIconManager
    {
        bool ShowOnlyOnClose { get; set; }
        bool MinimizeToTray { get; set; }
        bool CloseToTray { get; set; }
        bool ShowSynchronizedBalloon { get; set; }

        void Setup(INotifyIconDelegate rootViewModel);

        void EnsureIconVisible();
    }

    public class NotifyIconManager : INotifyIconManager
    {
        // Amount of time to squish 'synced' messages for after a connectivity event
        private static readonly TimeSpan syncedDeadTime = TimeSpan.FromSeconds(10);

        private readonly IViewManager viewManager;
        private readonly NotifyIconViewModel viewModel;
        private readonly IApplicationState application;
        private readonly ISyncThingManager syncThingManager;

        private INotifyIconDelegate rootViewModel;
        private TaskbarIcon taskbarIcon;

        private bool _showOnlyOnClose;
        public bool ShowOnlyOnClose
        {
            get { return this._showOnlyOnClose; }
            set
            {
                this._showOnlyOnClose = value;
                this.viewModel.Visible = !this._showOnlyOnClose || this.rootViewModel.State == ScreenState.Closed;
            }
        }

        public bool MinimizeToTray { get; set; }

        private bool _closeToTray;
        public bool CloseToTray
        {
            get { return this._closeToTray; }
            set { this._closeToTray = value; this.SetShutdownMode(); }
        }

        public bool ShowSynchronizedBalloon { get; set; }

        public NotifyIconManager(
            IViewManager viewManager,
            NotifyIconViewModel viewModel,
            IApplicationState application,
            ISyncThingManager syncThingManager)
        {
            this.viewManager = viewManager;
            this.viewModel = viewModel;
            this.application = application;
            this.syncThingManager = syncThingManager;

            this.viewModel.WindowOpenRequested += (o, e) =>
            {
                this.rootViewModel.EnsureInForeground();
            };
            this.viewModel.WindowCloseRequested += (o, e) => this.rootViewModel.CloseToTray();
            this.viewModel.ExitRequested += (o, e) => this.rootViewModel.Shutdown();

            this.syncThingManager.FolderSyncStateChanged += (o, e) =>
            {
                if (this.ShowSynchronizedBalloon &&
                    DateTime.UtcNow - this.syncThingManager.LastConnectivityEventTime > syncedDeadTime &&
                    e.SyncState == FolderSyncState.Idle && e.PrevSyncState == FolderSyncState.Syncing)
                {
                    Application.Current.Dispatcher.CheckAccess(); // Double-check
                    this.taskbarIcon.ShowBalloonTip("Finished Syncing", String.Format("{0}: Finished Syncing", e.Folder.FolderId), BalloonIcon.Info);
                }
            };
        }

        private void SetShutdownMode()
        {
            this.application.ShutdownMode = this._closeToTray ? ShutdownMode.OnExplicitShutdown : ShutdownMode.OnMainWindowClose;
        }

        public void Setup(INotifyIconDelegate rootViewModel)
        {
            this.rootViewModel = rootViewModel;

            this.taskbarIcon = (TaskbarIcon)this.application.FindResource("TaskbarIcon");
            this.viewManager.BindViewToModel(this.taskbarIcon, this.viewModel);

            this.rootViewModel.Activated += rootViewModelActivated;
            this.rootViewModel.Deactivated += rootViewModelDeactivated;
            this.rootViewModel.Closed += rootViewModelClosed;
        }

        public void EnsureIconVisible()
        {
            this.viewModel.Visible = true;
        }

        private void rootViewModelActivated(object sender, ActivationEventArgs e)
        {
            // If it's minimize to tray, not close to tray, then we'll have set the shutdown mode to OnExplicitShutdown just before closing
            // In this case, re-set Shutdownmode
            this.SetShutdownMode();

            this.viewModel.MainWindowVisible = true;
            if (this.ShowOnlyOnClose)
                this.viewModel.Visible = false;
        }

        private void rootViewModelDeactivated(object sender, DeactivationEventArgs e)
        {
            if (this.MinimizeToTray)
            {
                // Don't do this if it's shutting down
                if (this.application.HasMainWindow)
                    this.application.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                this.rootViewModel.CloseToTray();

                this.viewModel.MainWindowVisible = false;
                if (this.ShowOnlyOnClose)
                    this.viewModel.Visible = true;
            }
        }

        private void rootViewModelClosed(object sender, CloseEventArgs e)
        {
            this.viewModel.MainWindowVisible = false;
            if (this.ShowOnlyOnClose)
                this.viewModel.Visible = true;
        }
    }
}
