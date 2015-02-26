using NLog;
using Stylet;
using StyletIoC;
using SyncTrayzor.NotifyIcon;
using SyncTrayzor.Pages;
using SyncTrayzor.Services;
using SyncTrayzor.Services.UpdateChecker;
using SyncTrayzor.SyncThing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SyncTrayzor
{
    public class Bootstrapper : Bootstrapper<ShellViewModel>
    {
        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            builder.Bind<IApplicationState>().ToInstance(new ApplicationState(this.Application));
            builder.Bind<IConfigurationProvider>().To<ConfigurationProvider>().InSingletonScope();
            builder.Bind<IAutostartProvider>().To<AutostartProvider>().InSingletonScope();
            builder.Bind<ConfigurationApplicator>().ToSelf().InSingletonScope();
            builder.Bind<ISyncThingApiClient>().To<SyncThingApiClient>().InSingletonScope();
            builder.Bind<ISyncThingEventWatcher>().To<SyncThingEventWatcher>().InSingletonScope();
            builder.Bind<ISyncThingProcessRunner>().To<SyncThingProcessRunner>().InSingletonScope();
            builder.Bind<ISyncThingManager>().To<SyncThingManager>().InSingletonScope();
            builder.Bind<ISyncThingConnectionsWatcher>().To<SyncThingConnectionsWatcher>().InSingletonScope();
            builder.Bind<INotifyIconManager>().To<NotifyIconManager>().InSingletonScope();
            builder.Bind<IWatchedFolderMonitor>().To<WatchedFolderMonitor>().InSingletonScope();
            builder.Bind<IGithubApiClient>().To<GithubApiClient>().InSingletonScope();
            builder.Bind<IUpdateChecker>().To<UpdateChecker>().InSingletonScope();
        }

        protected override void Configure()
        {
            GlobalDiagnosticsContext.Set("LogFilePath", this.Container.Get<IConfigurationProvider>().BasePath);

            var notifyIconManager = this.Container.Get<INotifyIconManager>();
            notifyIconManager.Setup((INotifyIconDelegate)this.RootViewModel);
            this.Container.Get<ConfigurationApplicator>().ApplyConfiguration();
        }

        protected override void Launch()
        {
            if (this.Args.Length > 0 && this.Args[0] == "-minimized")
                this.Container.Get<INotifyIconManager>().EnsureIconVisible();
            else
                base.Launch();
        }

        protected override void OnLaunch()
        {
            var config = this.Container.Get<IConfigurationProvider>().Load();
            if (config.StartSyncthingAutomatically)
                ((ShellViewModel)this.RootViewModel).Start();

            // We don't care if this fails
            this.Container.Get<IUpdateChecker>().CheckForUpdatesAsync();
        }

        protected override void OnUnhandledException(DispatcherUnhandledExceptionEventArgs e)
        {
            var windowManager = this.Container.Get<IWindowManager>();
            var logger = LogManager.GetCurrentClassLogger();
            logger.Error("An unhandled exception occurred", e.Exception);

            var configurationException = e.Exception as ConfigurationException;
            if (configurationException != null)
            {
                windowManager.ShowMessageBox(String.Format("Configuration Error: {0}", configurationException.Message), "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                e.Handled = true;
            }
            else
            {
                windowManager.ShowMessageBox(String.Format("Unhandled error: {0}", e.Exception.Message), "Unhandled error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
