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
        ShutdownMode ShutdownMode { get; set; }
        bool HasMainWindow { get; }
        object FindResource(object resourceKey);
        void Shutdown();
    }

    public class ApplicationState : IApplicationState
    {
        private readonly Application application;

        public ApplicationState(Application application)
        {
            this.application = application;
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
    }
}
