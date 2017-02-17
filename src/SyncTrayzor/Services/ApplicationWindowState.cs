using Stylet;
using SyncTrayzor.Pages;
using System;

namespace SyncTrayzor.Services
{
    public interface IApplicationWindowState : IDisposable
    {
        event EventHandler<ActivationEventArgs> RootWindowActivated;
        event EventHandler<DeactivationEventArgs> RootWindowDeactivated;
        event EventHandler<CloseEventArgs> RootWindowClosed;

        ScreenState ScreenState { get; }

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

            this.rootViewModel.Activated += this.OnRootWindowActivated;
            this.rootViewModel.Deactivated += this.OnRootWindowDeactivated;
            this.rootViewModel.Closed += this.OnRootWindowClosed;
        }

        public event EventHandler<ActivationEventArgs> RootWindowActivated;
        public event EventHandler<DeactivationEventArgs> RootWindowDeactivated;
        public event EventHandler<CloseEventArgs> RootWindowClosed;

        private void OnRootWindowActivated(object sender, ActivationEventArgs e)
        {
            this.RootWindowActivated?.Invoke(this, e);
        }

        private void OnRootWindowDeactivated(object sender, DeactivationEventArgs e)
        {
            this.RootWindowDeactivated?.Invoke(this, e);
        }

        private void OnRootWindowClosed(object sender, CloseEventArgs e)
        {
            this.RootWindowClosed?.Invoke(this, e);
        }

        public ScreenState ScreenState => this.rootViewModel.ScreenState;

        public void CloseToTray()
        {
            this.rootViewModel.CloseToTray();
        }

        public void EnsureInForeground()
        {
            this.rootViewModel.EnsureInForeground();
        }

        public void Dispose()
        {
            this.rootViewModel.Activated -= this.OnRootWindowActivated;
            this.rootViewModel.Deactivated -= this.OnRootWindowDeactivated;
            this.rootViewModel.Closed -= this.OnRootWindowClosed;
        }
    }
}
