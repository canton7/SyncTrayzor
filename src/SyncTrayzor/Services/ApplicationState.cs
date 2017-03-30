using Microsoft.Win32;
using System;
using System.Windows;

namespace SyncTrayzor.Services
{
    public interface IApplicationState
    {
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

        public event EventHandler Startup;
        public event EventHandler ResumeFromSleep;

        public ApplicationState(Application application)
        {
            this.application = application;

            SystemEvents.PowerModeChanged += (o, e) =>
            {
                if (e.Mode == PowerModes.Resume)
                    this.OnResumeFromSleep();
            };
        }

        public ShutdownMode ShutdownMode
        {
            get => this.application.ShutdownMode;
            set
            {
                // This will fail if we're shutting down
                try
                {
                    this.application.ShutdownMode = value;
                }
                catch (InvalidOperationException) { }
            }
        }

        public bool HasMainWindow => this.application.MainWindow != null;

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
            this.Startup?.Invoke(this, EventArgs.Empty);
        }

        private void OnResumeFromSleep()
        {
            this.ResumeFromSleep?.Invoke(this, EventArgs.Empty);
        }
    }
}
