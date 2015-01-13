using Stylet;
using StyletIoC;
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
        }

        protected override void OnExit(System.Windows.ExitEventArgs e)
        {
            this.Container.Dispose();
        }
    }
}
