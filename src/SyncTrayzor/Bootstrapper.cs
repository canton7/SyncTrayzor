using Stylet;
using StyletIoC;
using SyncTrayzor.Pages;
using SyncTrayzor.Services;
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
            builder.Bind<ISyncThingRunner>().To<SyncThingRunner>().InSingletonScope();
        }
    }
}
