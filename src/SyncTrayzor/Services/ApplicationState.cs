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
