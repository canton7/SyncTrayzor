using Stylet;
using StyletIoC;
using SyncTrayzor.NotifyIcon;
using SyncTrayzor.Pages;
using SyncTrayzor.Services;
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
            builder.Bind<IApplicationState>().ToInstance(new ApplicationState(this.Application));
            builder.Bind<IConfigurationProvider>().To<ConfigurationProvider>().InSingletonScope();
            builder.Bind<AutostartProvider>().ToSelf().InSingletonScope();
            builder.Bind<ConfigurationApplicator>().ToSelf().InSingletonScope();
            builder.Bind<ISyncThingApiClient>().To<SyncThingApiClient>().InSingletonScope();
            builder.Bind<ISyncThingEventWatcher>().To<SyncThingEventWatcher>().InSingletonScope();
            builder.Bind<ISyncThingProcessRunner>().To<SyncThingProcessRunner>().InSingletonScope();
            builder.Bind<ISyncThingManager>().To<SyncThingManager>().InSingletonScope();
            builder.Bind<INotifyIconManager>().To<NotifyIconManager>().InSingletonScope();
            builder.Bind<IWatchedFolderMonitor>().To<WatchedFolderMonitor>().InSingletonScope();
        }

        protected override void Launch()
        {
            var notifyIconManager = this.Container.Get<INotifyIconManager>();
            notifyIconManager.Setup((INotifyIconDelegate)this.RootViewModel);
            this.Container.Get<ConfigurationApplicator>().ApplyConfiguration();

            if (this.Args.Length > 0 && this.Args[0] == "-minimized")
                this.Container.Get<INotifyIconManager>().EnsureIconVisible();
            else
                base.Launch();

            var config = this.Container.Get<IConfigurationProvider>().Load();
            if (config.StartSyncThingAutomatically)
                ((ShellViewModel)this.RootViewModel).Start();
        }

        protected override void OnExit(System.Windows.ExitEventArgs e)
        {
            this.Container.Dispose();
        }
    }
}
