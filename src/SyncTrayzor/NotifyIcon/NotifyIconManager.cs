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
        void Setup(Screen rootViewModel, Application application);

        void HideToTray();
        void Show();
        void ShowBaloonTip(string title, string text);
    }

    public class NotifyIconManager : INotifyIconManager
    {
        private readonly IWindowManager windowManager;
        private Screen rootViewModel;
        private Application application;
        private TaskbarIcon taskbarIcon;

        public NotifyIconManager(IWindowManager windowManager)
        {
            this.windowManager = windowManager;
        }

        public void Setup(Screen rootViewModel, Application application)
        {
            this.rootViewModel = rootViewModel;
            this.application = application;

            this.taskbarIcon = (TaskbarIcon)this.application.FindResource("TaskbarIcon");
        }

        public void HideToTray()
        {
            this.rootViewModel.TryClose();
        }

        public void Show()
        {
            this.windowManager.ShowWindow(this.rootViewModel);
        }

        public void ShowBaloonTip(string title, string text)
        {
            this.taskbarIcon.ShowBalloonTip(title, text, BalloonIcon.Info);
        }
    }
}
