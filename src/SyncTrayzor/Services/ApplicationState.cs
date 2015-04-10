using Microsoft.Win32;
using Stylet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SyncTrayzor.Services
{
    public interface IApplicationState
    {
        event EventHandler<ActivationEventArgs> RootWindowActivated;
        event EventHandler<DeactivationEventArgs> RootWindowDeactivated;
        event EventHandler<CloseEventArgs> RootWindowClosed;
        event EventHandler Startup;
        event EventHandler ResumeFromSleep;

        void ApplicationStarted();
        ShutdownMode ShutdownMode { get; set; }
        bool HasMainWindow { get; }
        object FindResource(object resourceKey);
        void Shutdown();
    }

    public class ApplicationState : IApplicationState
    {
        private readonly Application application;
        private readonly IScreenState rootViewModel;

        public event EventHandler<ActivationEventArgs> RootWindowActivated
        {
            add { this.rootViewModel.Activated += value; }
            remove { this.rootViewModel.Activated -= value; }
        }

        public event EventHandler<DeactivationEventArgs> RootWindowDeactivated
        {
            add { this.rootViewModel.Deactivated += value; }
            remove { this.rootViewModel.Deactivated -= value; }
        }

        public event EventHandler<CloseEventArgs> RootWindowClosed
        {
            add { this.rootViewModel.Closed += value; }
            remove { this.rootViewModel.Closed -= value; }
        }

        public event EventHandler Startup;
        public event EventHandler ResumeFromSleep;

        public ApplicationState(Application application, IScreenState rootViewModel)
        {
            this.application = application;
            this.rootViewModel = rootViewModel;

            SystemEvents.PowerModeChanged += (o, e) =>
            {
                if (e.Mode == PowerModes.Resume)
                    this.OnResumeFromSleep();
            };
        }

        public ShutdownMode ShutdownMode
        {
            get { return this.application.ShutdownMode; }
            set { this.application.ShutdownMode = value; }
        }

        public bool HasMainWindow
        {
            get { return this.application.MainWindow != null; }
        }

        public object FindResource(object resourceKey)
        {
            return this.application.FindResource(resourceKey);
        }

        public void Shutdown()
        {
            this.application.Shutdown();
        }

        public void ApplicationStarted()
        {
            var handler = this.Startup;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private void OnResumeFromSleep()
        {
            var handler = this.ResumeFromSleep;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }
    }
}
