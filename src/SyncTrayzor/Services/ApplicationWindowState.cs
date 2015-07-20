using Stylet;
using SyncTrayzor.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services
{
    public interface IApplicationWindowState
    {
        event EventHandler<ActivationEventArgs> RootWindowActivated;
        event EventHandler<DeactivationEventArgs> RootWindowDeactivated;
        event EventHandler<CloseEventArgs> RootWindowClosed;

        ScreenState State { get; }

        void Setup(ShellViewModel rootViewModel);

        void CloseToTray();
        void EnsureInForeground();
    }

    public class ApplicationWindowState : IApplicationWindowState
    {
        private ShellViewModel rootViewModel;

        public void Setup(ShellViewModel rootViewModel)
        {
            this.rootViewModel = rootViewModel;

            this.rootViewModel.Activated += (o, e) => this.OnRootWindowActivated(e);
            this.rootViewModel.Deactivated += (o, e) => this.OnRootWindowDeactivated(e);
            this.rootViewModel.Closed += (o, e) => this.OnRootWindowClosed(e);
        }

        public event EventHandler<ActivationEventArgs> RootWindowActivated;
        public event EventHandler<DeactivationEventArgs> RootWindowDeactivated;
        public event EventHandler<CloseEventArgs> RootWindowClosed;

        private void OnRootWindowActivated(ActivationEventArgs e)
        {
            this.RootWindowActivated?.Invoke(this, e);
        }

        private void OnRootWindowDeactivated(DeactivationEventArgs e)
        {
            this.RootWindowDeactivated?.Invoke(this, e);
        }

        private void OnRootWindowClosed(CloseEventArgs e)
        {
            this.RootWindowClosed?.Invoke(this, e);
        }

        public ScreenState State
        {
            get { return this.rootViewModel.State; }
        }

        public void CloseToTray()
        {
            this.rootViewModel.CloseToTray();
        }

        public void EnsureInForeground()
        {
            this.rootViewModel.EnsureInForeground();
        }
    }
}
