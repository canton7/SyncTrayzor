using Hardcodet.Wpf.TaskbarNotification;
using Stylet;
using SyncTrayzor.Localization;
using SyncTrayzor.Services;
using SyncTrayzor.SyncThing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
        bool ShowDeviceConnectivityBalloons { get; set; }

        void EnsureIconVisible();

        Task<bool?> ShowBalloonAsync(object viewModel, CancellationToken cancellationToken);
    }

    public class NotifyIconManager : INotifyIconManager
    {
        // Amount of time to squish 'synced' messages for after a connectivity event
        private static readonly TimeSpan syncedDeadTime = TimeSpan.FromSeconds(10);

        private readonly IViewManager viewManager;
        private readonly NotifyIconViewModel viewModel;
        private readonly IApplicationState application;
        private readonly IApplicationWindowState applicationWindowState;
        private readonly ISyncThingManager syncThingManager;

        private TaskbarIcon taskbarIcon;

        private TaskCompletionSource<bool?> balloonTcs;

        private bool _showOnlyOnClose;
        public bool ShowOnlyOnClose
        {
            get { return this._showOnlyOnClose; }
            set
            {
                this._showOnlyOnClose = value;
                this.viewModel.Visible = !this._showOnlyOnClose || this.applicationWindowState.State == ScreenState.Closed;
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
        public bool ShowDeviceConnectivityBalloons { get; set; }

        public NotifyIconManager(
            IViewManager viewManager,
            NotifyIconViewModel viewModel,
            IApplicationState application,
            IApplicationWindowState applicationWindowState,
            ISyncThingManager syncThingManager)
        {
            this.viewManager = viewManager;
            this.viewModel = viewModel;
            this.application = application;
            this.applicationWindowState = applicationWindowState;
            this.syncThingManager = syncThingManager;

            this.taskbarIcon = (TaskbarIcon)this.application.FindResource("TaskbarIcon");
            this.viewManager.BindViewToModel(this.taskbarIcon, this.viewModel);

            this.applicationWindowState.RootWindowActivated += this.RootViewModelActivated;
            this.applicationWindowState.RootWindowDeactivated += this.RootViewModelDeactivated;
            this.applicationWindowState.RootWindowClosed += this.RootViewModelClosed; 

            this.viewModel.WindowOpenRequested += (o, e) =>
            {
                this.applicationWindowState.EnsureInForeground();
            };
            this.viewModel.WindowCloseRequested += (o, e) =>
            {
                // Always minimize, regardless of settings
                this.application.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                this.applicationWindowState.CloseToTray();
            };
            this.viewModel.ExitRequested += (o, e) => this.application.Shutdown();

            this.syncThingManager.FolderSyncStateChanged += (o, e) =>
            {
                if (this.ShowSynchronizedBalloon &&
                    DateTime.UtcNow - this.syncThingManager.LastConnectivityEventTime > syncedDeadTime &&
                    DateTime.UtcNow - this.syncThingManager.StartedTime > syncedDeadTime &&
                    e.SyncState == FolderSyncState.Idle && e.PrevSyncState == FolderSyncState.Syncing)
                {
                    Application.Current.Dispatcher.CheckAccess(); // Double-check
                    this.taskbarIcon.ShowBalloonTip(Localizer.Translate("TrayIcon_Balloon_FinishedSyncing_Title"), Localizer.Translate("TrayIcon_Balloon_FinishedSyncing_Message", e.Folder.FolderId), BalloonIcon.Info);
                }
            };

            this.syncThingManager.DeviceConnected += (o, e) =>
            {
                if (this.ShowDeviceConnectivityBalloons &&
                    DateTime.UtcNow - this.syncThingManager.StartedTime > syncedDeadTime)
                {
                    this.taskbarIcon.ShowBalloonTip(Localizer.Translate("TrayIcon_Balloon_DeviceConnected_Title"), Localizer.Translate("TrayIcon_Balloon_DeviceConnected_Message", e.Device.Name), BalloonIcon.Info);
                }
            };

            this.syncThingManager.DeviceDisconnected += (o, e) =>
            {
                if (this.ShowDeviceConnectivityBalloons &&
                    DateTime.UtcNow - this.syncThingManager.StartedTime > syncedDeadTime)
                {
                    this.taskbarIcon.ShowBalloonTip(Localizer.Translate("TrayIcon_Balloon_DeviceDisconnected_Title"), Localizer.Translate("TrayIcon_Balloon_DeviceDisconnected_Message", e.Device.Name), BalloonIcon.Info);
                }
            };
        }

        private void SetShutdownMode()
        {
            this.application.ShutdownMode = this._closeToTray ? ShutdownMode.OnExplicitShutdown : ShutdownMode.OnMainWindowClose;
        }

        public async Task<bool?> ShowBalloonAsync(object viewModel, CancellationToken cancellationToken)
        {
            this.CloseCurrentlyOpenBalloon(cancel: false);

            var view = this.viewManager.CreateViewForModel(viewModel);
            this.taskbarIcon.ShowCustomBalloon(view, System.Windows.Controls.Primitives.PopupAnimation.Slide, null);
            this.viewManager.BindViewToModel(view, viewModel); // Re-assign DataContext, after NotifyIcon overwrote it ><

            this.balloonTcs = new TaskCompletionSource<bool?>();
            new BalloonConductor(this.taskbarIcon, viewModel, view, this.balloonTcs);

            using (cancellationToken.Register(() =>
            {
                if (this.taskbarIcon.CustomBalloon.Child == view)
                    this.CloseCurrentlyOpenBalloon(cancel: true);
            }))
            {
                return await this.balloonTcs.Task;
            }
        }

        private void CloseCurrentlyOpenBalloon(bool cancel)
        {
            if (this.balloonTcs == null)
                return;

            this.taskbarIcon.CloseBalloon();

            if (cancel)
                this.balloonTcs.TrySetCanceled();
            else
                this.balloonTcs.TrySetResult(null);

            this.balloonTcs = null;
        }

        public void EnsureIconVisible()
        {
            this.viewModel.Visible = true;
        }

        private void RootViewModelActivated(object sender, ActivationEventArgs e)
        {
            // If it's minimize to tray, not close to tray, then we'll have set the shutdown mode to OnExplicitShutdown just before closing
            // In this case, re-set Shutdownmode
            this.SetShutdownMode();

            this.viewModel.MainWindowVisible = true;
            if (this.ShowOnlyOnClose)
                this.viewModel.Visible = false;
        }

        private void RootViewModelDeactivated(object sender, DeactivationEventArgs e)
        {
            if (this.MinimizeToTray)
            {
                // Don't do this if it's shutting down
                if (this.application.HasMainWindow)
                    this.application.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                this.applicationWindowState.CloseToTray();

                this.viewModel.MainWindowVisible = false;
                if (this.ShowOnlyOnClose)
                    this.viewModel.Visible = true;
            }
        }

        private void RootViewModelClosed(object sender, CloseEventArgs e)
        {
            this.viewModel.MainWindowVisible = false;
            if (this.ShowOnlyOnClose)
                this.viewModel.Visible = true;
        }
    }
}
