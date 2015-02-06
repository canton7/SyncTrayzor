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
        bool CloseToTray { get; set; }
        bool ShowSynchronizedBalloon { get; set; }

        void Setup(INotifyIconDelegate rootViewModel);

        void EnsureIconVisible();
    }

    public class NotifyIconManager : INotifyIconManager
    {
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
                this.viewModel.Visible = !this._showOnlyOnClose; // Assume this setting can only be changed when the RootViewModel isn't closed
            }
        }

        private bool _closeToTray;
        public bool CloseToTray
        {
            get { return this._closeToTray; }
            set
            {
                this._closeToTray = value;
                this.application.ShutdownMode = this._closeToTray ? ShutdownMode.OnExplicitShutdown : ShutdownMode.OnMainWindowClose;
            }
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
                if (!this.application.HasMainWindow)
                    this.rootViewModel.RestoreFromTray();
            };
            this.viewModel.WindowCloseRequested += (o, e) => this.rootViewModel.CloseToTray();
            this.viewModel.ExitRequested += (o, e) => this.rootViewModel.Shutdown();

            this.syncThingManager.FolderSyncStateChanged += (o, e) =>
            {
                if (this.ShowSynchronizedBalloon && this.syncThingManager.StartedAt.HasValue &&
                    DateTime.UtcNow - this.syncThingManager.StartedAt.Value < TimeSpan.FromSeconds(60) &&
                    e.SyncState == FolderSyncState.Idle && e.PrevSyncState == FolderSyncState.Syncing)
                {
                    Application.Current.Dispatcher.CheckAccess(); // Double-check
                    this.taskbarIcon.ShowBalloonTip("Finished Syncing", String.Format("{0}: Finished Syncing", e.Folder.FolderId), BalloonIcon.Info);
                }
            };
        }

        public void Setup(INotifyIconDelegate rootViewModel)
        {
            this.rootViewModel = rootViewModel;

            this.taskbarIcon = (TaskbarIcon)this.application.FindResource("TaskbarIcon");
            this.viewManager.BindViewToModel(this.taskbarIcon, this.viewModel);

            this.rootViewModel.Activated += rootViewModelActivated;
            this.rootViewModel.Closed += rootViewModelClosed;
        }

        public void EnsureIconVisible()
        {
            this.viewModel.Visible = true;
        }

        private void rootViewModelActivated(object sender, ActivationEventArgs e)
        {
            this.viewModel.MainWindowVisible = true;
            if (this.ShowOnlyOnClose)
                this.viewModel.Visible = false;
        }

        private void rootViewModelClosed(object sender, CloseEventArgs e)
        {
            this.viewModel.MainWindowVisible = false;
            if (this.ShowOnlyOnClose)
                this.viewModel.Visible = true;
        }
    }
}
