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
        protected override void Configure()
        {
            Stylet.Logging.LogManager.Enabled = true;
        }

        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            builder.Bind<ISyncThingApiClient>().To<SyncThingApiClient>();
            builder.Bind<ISyncThingProcessRunner>().To<SyncThingProcessRunner>();
            builder.Bind<ISyncThingManager>().To<SyncThingManager>().InSingletonScope();
            builder.Bind<INotifyIconManager>().To<NotifyIconManager>().InSingletonScope();
        }

        protected override void OnStartup()
        {
            var notifyIconManager = this.Container.Get<INotifyIconManager>();
            notifyIconManager.Setup((ShellViewModel)this.RootViewModel, this.Application);
        }

        protected override void OnExit(System.Windows.ExitEventArgs e)
        {
            this.Container.Dispose();
        }
    }
}
