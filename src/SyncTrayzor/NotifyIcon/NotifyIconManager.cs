using Hardcodet.Wpf.TaskbarNotification;
using Stylet;
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

        void Setup(IScreen rootViewModel, Application application);

        void HideToTray();
        void Show();
        void ShowBaloonTip(string title, string text);
    }

    public class NotifyIconManager : INotifyIconManager
    {
        private readonly IWindowManager windowManager;
        private readonly IViewManager viewManager;
        private readonly NotifyIconViewModel viewModel;

        private IScreen rootViewModel;
        private Application application;
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

        public NotifyIconManager(IWindowManager windowManager, IViewManager viewManager, NotifyIconViewModel viewModel)
        {
            this.windowManager = windowManager;
            this.viewManager = viewManager;
            this.viewModel = viewModel;

            this.viewModel.WindowOpenRequested += (o, e) =>
            {
                if (this.application.MainWindow == null)
                    this.windowManager.ShowWindow(this.rootViewModel);
            };
            this.viewModel.ExitRequested += (o, e) => this.application.Shutdown();
        }

        public void Setup(IScreen rootViewModel, Application application)
        {
            this.rootViewModel = rootViewModel;
            this.application = application;

            this.taskbarIcon = (TaskbarIcon)this.application.FindResource("TaskbarIcon");
            this.viewManager.BindViewToModel(this.taskbarIcon, this.viewModel);

            this.rootViewModel.Activated += rootViewModelActivated;
            this.rootViewModel.Closed += rootViewModelClosed;

            this.ShowOnlyOnClose = false;
            this.CloseToTray = true;
        }

        public void HideToTray()
        {
            this.rootViewModel.RequestClose();
        }

        public void Show()
        {
            this.windowManager.ShowWindow(this.rootViewModel);
        }

        public void ShowBaloonTip(string title, string text)
        {
            this.taskbarIcon.ShowBalloonTip(title, text, BalloonIcon.Info);
        }

        private void rootViewModelActivated(object sender, ActivationEventArgs e)
        {
            if (this.ShowOnlyOnClose)
                this.viewModel.Visible = false;
        }

        private void rootViewModelClosed(object sender, CloseEventArgs e)
        {
            if (this.ShowOnlyOnClose)
                this.viewModel.Visible = true;
        }
    }
}
