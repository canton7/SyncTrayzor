using Stylet;
using StyletIoC;
using SyncTrayzor.NotifyIcon;
using SyncTrayzor.Pages;
using SyncTrayzor.SyncThing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor
{
    public class Bootstrapper : Bootstrapper<ShellViewModel>
    {
        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            builder.Bind<ISyncThingApiClient>().To<SyncThingApiClient>();
            builder.Bind<ISyncThingProcessRunner>().To<SyncThingProcessRunner>();
            builder.Bind<ISyncThingManager>().To<SyncThingManager>().InSingletonScope();
            builder.Bind<INotifyIconManager>().To<NotifyIconManager>().InSingletonScope();
        }

        protected override void Launch()
        {
            // Override how launching is done
            var notifyIconManager = this.Container.Get<INotifyIconManager>();
            var rootViewModel = this.Container.Get<ShellViewModel>();
            var windowManager = this.Container.Get<IWindowManager>();

            notifyIconManager.Setup(rootViewModel, this.Application);

            windowManager.ShowWindow(rootViewModel);
        }

        protected override void OnExit(System.Windows.ExitEventArgs e)
        {
            this.Container.Dispose();
        }
    }
}
