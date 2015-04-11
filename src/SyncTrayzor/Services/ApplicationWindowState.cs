using Stylet;
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
    }

    public class ApplicationWindowState : IApplicationWindowState
    {
        private readonly IScreenState rootViewModel;

        public ApplicationWindowState(IScreenState rootViewModel)
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
    }
}
