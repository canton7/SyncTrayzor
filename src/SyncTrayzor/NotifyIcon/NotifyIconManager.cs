using Hardcodet.Wpf.TaskbarNotification;
using Stylet;
using SyncTrayzor.Localization;
using SyncTrayzor.Properties;
using SyncTrayzor.Services;
using SyncTrayzor.Syncthing;
using SyncTrayzor.Syncthing.ApiClient;
using SyncTrayzor.Syncthing.Devices;
using SyncTrayzor.Syncthing.TransferHistory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SyncTrayzor.NotifyIcon
{
    public interface INotifyIconManager : IDisposable
    {
        bool ShowOnlyOnClose { get; set; }
        bool MinimizeToTray { get; set; }
        bool CloseToTray { get; set; }
        Dictionary<string, bool> FolderNotificationsEnabled { get; set; }
        bool ShowSynchronizedBalloonEvenIfNothingDownloaded { get; set; }
        bool ShowDeviceConnectivityBalloons { get; set; }
        bool ShowDeviceOrFolderRejectedBalloons { get; set; }

        void EnsureIconVisible();

        Task<bool?> ShowBalloonAsync(object viewModel, int? timeout = null, CancellationToken? cancellationToken = null);
    }

    public class NotifyIconManager : INotifyIconManager
    {
        // Amount of time to squish 'synced' messages for after a connectivity event
        private static readonly TimeSpan syncedDeadTime = TimeSpan.FromSeconds(10);

        private readonly IViewManager viewManager;
        private readonly NotifyIconViewModel viewModel;
        private readonly IApplicationState application;
        private readonly IApplicationWindowState applicationWindowState;
        private readonly ISyncthingManager syncthingManager;
        private readonly IConnectedEventDebouncer connectedEventDebouncer;

        private TaskbarIcon taskbarIcon;

        private TaskCompletionSource<bool?> balloonTcs;

        private bool _showOnlyOnClose;
        public bool ShowOnlyOnClose
        {
            get { return this._showOnlyOnClose; }
            set
            {
                this._showOnlyOnClose = value;
                this.viewModel.Visible = !this._showOnlyOnClose || this.applicationWindowState.ScreenState == ScreenState.Closed;
            }
        }

        public bool MinimizeToTray { get; set; }

        private bool _closeToTray;
        public bool CloseToTray
        {
            get { return this._closeToTray; }
            set { this._closeToTray = value; this.SetShutdownMode(); }
        }

        // FolderId -> is enabled
        public Dictionary<string, bool> FolderNotificationsEnabled { get; set; }
        public bool ShowSynchronizedBalloonEvenIfNothingDownloaded { get; set; }
        public bool ShowDeviceConnectivityBalloons { get; set; }
        public bool ShowDeviceOrFolderRejectedBalloons { get; set; }

        public NotifyIconManager(
            IViewManager viewManager,
            NotifyIconViewModel viewModel,
            IApplicationState application,
            IApplicationWindowState applicationWindowState,
            ISyncthingManager syncthingManager,
            IConnectedEventDebouncer connectedEventDebouncer)
        {
            this.viewManager = viewManager;
            this.viewModel = viewModel;
            this.application = application;
            this.applicationWindowState = applicationWindowState;
            this.syncthingManager = syncthingManager;
            this.connectedEventDebouncer = connectedEventDebouncer;

            this.taskbarIcon = (TaskbarIcon)this.application.FindResource("TaskbarIcon");
            this.taskbarIcon.TrayBalloonTipClicked += (o, e) =>
            {
                this.applicationWindowState.EnsureInForeground();
            };

            // Need to hold off until after the application is started, otherwise the ViewManager won't be set
            this.application.Startup += this.ApplicationStartup;

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

            this.syncthingManager.TransferHistory.FolderSynchronizationFinished += this.FolderSynchronizationFinished;
            this.syncthingManager.Devices.DeviceConnected += this.DeviceConnected;
            this.syncthingManager.Devices.DeviceDisconnected += this.DeviceDisconnected;
            this.syncthingManager.DeviceRejected += this.DeviceRejected;
            this.syncthingManager.FolderRejected += this.FolderRejected;

            this.connectedEventDebouncer.DeviceConnected += this.DebouncedDeviceConnected;
        }

        private void ApplicationStartup(object sender, EventArgs e)
        {
            this.viewManager.BindViewToModel(this.taskbarIcon, this.viewModel);
        }

        private void DeviceConnected(object sender, DeviceConnectedEventArgs e)
        {
            if (this.ShowDeviceConnectivityBalloons &&
                    DateTime.UtcNow - this.syncthingManager.StartedTime > syncedDeadTime)
            {
                this.connectedEventDebouncer.Connect(e.Device);
            }
        }

        private void DebouncedDeviceConnected(object sender, DeviceConnectedEventArgs e)
        {
            this.taskbarIcon.HideBalloonTip();
            this.taskbarIcon.ShowBalloonTip(Resources.TrayIcon_Balloon_DeviceConnected_Title, Localizer.F(Resources.TrayIcon_Balloon_DeviceConnected_Message, e.Device.Name), BalloonIcon.Info);
        }

        private void DeviceDisconnected(object sender, DeviceDisconnectedEventArgs e)
        {
            if (this.ShowDeviceConnectivityBalloons &&
                    DateTime.UtcNow - this.syncthingManager.StartedTime > syncedDeadTime)
            {
                if (this.connectedEventDebouncer.Disconnect(e.Device))
                {
                    this.taskbarIcon.HideBalloonTip();
                    this.taskbarIcon.ShowBalloonTip(Resources.TrayIcon_Balloon_DeviceDisconnected_Title, Localizer.F(Resources.TrayIcon_Balloon_DeviceDisconnected_Message, e.Device.Name), BalloonIcon.Info);
                }
            }
        }

        private void DeviceRejected(object sender, DeviceRejectedEventArgs e)
        {
            if (this.ShowDeviceOrFolderRejectedBalloons)
            {
                this.taskbarIcon.HideBalloonTip();
                this.taskbarIcon.ShowBalloonTip(Resources.TrayIcon_Balloon_DeviceRejected_Title, Localizer.F(Resources.TrayIcon_Balloon_DeviceRejected_Message, e.DeviceId, e.Address), BalloonIcon.Info);
            }
        }

        private void FolderRejected(object sender, FolderRejectedEventArgs e)
        {
            if (this.ShowDeviceOrFolderRejectedBalloons)
            {
                this.taskbarIcon.HideBalloonTip();
                this.taskbarIcon.ShowBalloonTip(Resources.TrayIcon_Balloon_FolderRejected_Title, Localizer.F(Resources.TrayIcon_Balloon_FolderRejected_Message, e.Device.Name, e.Folder.Label), BalloonIcon.Info);
            }
        }

        private void FolderSynchronizationFinished(object sender, FolderSynchronizationFinishedEventArgs e)
        {
            // If it only contains failed transfers we've seen before, then we don't care.
            // Otherwise we'll keep bugging the user (every minute) for a failing transfer. 
            // However, with this behaviour, we'll still remind them about the failure whenever something succeeds (or a new failure is added)
            if (e.FileTransfers.All(x => x.Error != null && !x.IsNewError))
                return;

            bool notificationsEnabled;
            if (this.FolderNotificationsEnabled != null && this.FolderNotificationsEnabled.TryGetValue(e.Folder.FolderId, out notificationsEnabled) && notificationsEnabled)
            {
                if (e.FileTransfers.Count == 0)
                {
                    if (this.ShowSynchronizedBalloonEvenIfNothingDownloaded &&
                        DateTime.UtcNow - this.syncthingManager.LastConnectivityEventTime > syncedDeadTime &&
                        DateTime.UtcNow - this.syncthingManager.StartedTime > syncedDeadTime)
                    {
                        this.taskbarIcon.HideBalloonTip();
                        this.taskbarIcon.ShowBalloonTip(Resources.TrayIcon_Balloon_FinishedSyncing_Title, String.Format(Resources.TrayIcon_Balloon_FinishedSyncing_Message, e.Folder.Label), BalloonIcon.Info);
                    }
                }
                else if (e.FileTransfers.Count == 1)
                {
                    var fileTransfer = e.FileTransfers[0];
                    string msg = null;
                    if (fileTransfer.Error == null)
                    { 
                        if (fileTransfer.ActionType == ItemChangedActionType.Update)
                            msg = Localizer.F(Resources.TrayIcon_Balloon_FinishedSyncing_UpdatedSingleFile, e.Folder.Label, Path.GetFileName(fileTransfer.Path));
                        else if (fileTransfer.ActionType == ItemChangedActionType.Delete)
                            msg = Localizer.F(Resources.TrayIcon_Balloon_FinishedSyncing_DeletedSingleFile, e.Folder.Label, Path.GetFileName(fileTransfer.Path));
                    }
                    else
                    {
                        if (fileTransfer.ActionType == ItemChangedActionType.Update)
                            msg = Localizer.F(Resources.TrayIcon_Balloon_FinishedSyncing_FailedToUpdateSingleFile, e.Folder.Label, Path.GetFileName(fileTransfer.Path), fileTransfer.Error);
                        else if (fileTransfer.ActionType == ItemChangedActionType.Delete)
                            msg = Localizer.F(Resources.TrayIcon_Balloon_FinishedSyncing_FailedToDeleteSingleFile, e.Folder.Label, Path.GetFileName(fileTransfer.Path), fileTransfer.Error);
                    }

                    if (msg != null)
                    {
                        this.taskbarIcon.HideBalloonTip();
                        this.taskbarIcon.ShowBalloonTip(Resources.TrayIcon_Balloon_FinishedSyncing_Title, msg, BalloonIcon.Info);
                    }
                }
                else
                {
                    var updates = e.FileTransfers.Where(x => x.ActionType == ItemChangedActionType.Update).ToArray();
                    var deletes = e.FileTransfers.Where(x => x.ActionType == ItemChangedActionType.Delete).ToArray();

                    var messageParts = new List<string>();

                    if (updates.Length > 0)
                    {
                        var failureCount = updates.Count(x => x.Error != null);
                        if (failureCount > 0)
                            messageParts.Add(Localizer.F(Resources.TrayIcon_Balloon_FinishedSyncing_UpdatedFileWithFailures, updates.Length, failureCount));
                        else
                            messageParts.Add(Localizer.F(Resources.TrayIcon_Balloon_FinishedSyncing_UpdatedFile, updates.Length));
                    }
                        

                    if (deletes.Length > 0)
                    {
                        var failureCount = deletes.Count(x => x.Error != null);
                        if (failureCount > 0)
                            messageParts.Add(Localizer.F(Resources.TrayIcon_Balloon_FinishedSyncing_DeletedFileWithFailures, deletes.Length, failureCount));
                        else
                            messageParts.Add(Localizer.F(Resources.TrayIcon_Balloon_FinishedSyncing_DeletedFile, deletes.Length));
                    }
                        
                    var text = Localizer.F(Resources.TrayIcon_Balloon_FinishedSyncing_Multiple, e.Folder.Label, messageParts);

                    this.taskbarIcon.HideBalloonTip();
                    this.taskbarIcon.ShowBalloonTip(Resources.TrayIcon_Balloon_FinishedSyncing_Title, text, BalloonIcon.Info);
                }
            }
        }

        private void SetShutdownMode()
        {
            this.application.ShutdownMode = this._closeToTray ? ShutdownMode.OnExplicitShutdown : ShutdownMode.OnMainWindowClose;
        }

        public async Task<bool?> ShowBalloonAsync(object viewModel, int? timeout = null, CancellationToken? cancellationToken = null)
        {
            var token = cancellationToken ?? CancellationToken.None;

            this.CloseCurrentlyOpenBalloon(cancel: false);

            var view = this.viewManager.CreateViewForModel(viewModel);
            this.taskbarIcon.ShowCustomBalloon(view, System.Windows.Controls.Primitives.PopupAnimation.Slide, timeout);
            this.taskbarIcon.CustomBalloon.StaysOpen = false;
            this.viewManager.BindViewToModel(view, viewModel); // Re-assign DataContext, after NotifyIcon overwrote it ><

            this.balloonTcs = new TaskCompletionSource<bool?>();
            new BalloonConductor(this.taskbarIcon, viewModel, view, this.balloonTcs);

            using (token.Register(() =>
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

        public void Dispose()
        {
            this.application.Startup -= this.ApplicationStartup;

            this.applicationWindowState.RootWindowActivated -= this.RootViewModelActivated;
            this.applicationWindowState.RootWindowDeactivated -= this.RootViewModelDeactivated;
            this.applicationWindowState.RootWindowClosed -= this.RootViewModelClosed;

            this.syncthingManager.TransferHistory.FolderSynchronizationFinished -= this.FolderSynchronizationFinished;
            this.syncthingManager.Devices.DeviceConnected -= this.DeviceConnected;
            this.syncthingManager.Devices.DeviceDisconnected -= this.DeviceDisconnected;
            this.syncthingManager.DeviceRejected -= this.DeviceRejected;
            this.syncthingManager.FolderRejected -= this.FolderRejected;
        }
    }
}
