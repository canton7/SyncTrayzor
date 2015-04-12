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

        void CloseToTray();
        void EnsureInForeground();
    }

    public class ApplicationWindowState : IApplicationWindowState
    {
        private readonly ShellViewModel rootViewModel;

        public ApplicationWindowState(ShellViewModel rootViewModel)
        {
            this.rootViewModel = rootViewModel;
        }

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
