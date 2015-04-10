using CefSharp;
using FluentValidation;
using Microsoft.Win32;
using NLog;
using Stylet;
using StyletIoC;
using SyncTrayzor.Localization;
using SyncTrayzor.NotifyIcon;
using SyncTrayzor.Pages;
using SyncTrayzor.Properties;
using SyncTrayzor.Services;
using SyncTrayzor.Services.Config;
using SyncTrayzor.Services.UpdateManagement;
using SyncTrayzor.SyncThing;
using SyncTrayzor.SyncThing.ApiClient;
using SyncTrayzor.SyncThing.EventWatcher;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SyncTrayzor
{
    public class Bootstrapper : Bootstrapper<ShellViewModel>
    {
        private bool exiting;

        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            builder.Bind<IApplicationState>().ToInstance(new ApplicationState(this.Application));
            builder.Bind<IConfigurationProvider>().To<ConfigurationProvider>().InSingletonScope();
            builder.Bind<IAutostartProvider>().To<AutostartProvider>().InSingletonScope();
            builder.Bind<ConfigurationApplicator>().ToSelf().InSingletonScope();
            builder.Bind<ISyncThingApiClientFactory>().To<SyncThingApiClientFactory>();
            builder.Bind<ISyncThingEventWatcherFactory>().To<SyncThingEventWatcherFactory>();
            builder.Bind<ISyncThingProcessRunner>().To<SyncThingProcessRunner>().InSingletonScope();
            builder.Bind<ISyncThingManager>().To<SyncThingManager>().InSingletonScope();
            builder.Bind<ISyncThingConnectionsWatcherFactory>().To<SyncThingConnectionsWatcherFactory>();
            builder.Bind<INotifyIconManager>().To<NotifyIconManager>().InSingletonScope();
            builder.Bind<IWatchedFolderMonitor>().To<WatchedFolderMonitor>().InSingletonScope();
            builder.Bind<IGithubApiClient>().To<GithubApiClient>().InSingletonScope();
            builder.Bind<IUpdateManager>().To<UpdateManager>().InSingletonScope();
            builder.Bind<IUpdateChecker>().To<UpdateChecker>();
            builder.Bind<IProcessStartProvider>().To<ProcessStartProvider>().InSingletonScope();

            builder.Bind(typeof(IModelValidator<>)).To(typeof(FluentModelValidator<>));
            builder.Bind(typeof(IValidator<>)).ToAllImplementations(this.Assemblies);
        }

        protected override void Configure()
        {
            var pathConfiguration = Settings.Default.PathConfiguration;
            pathConfiguration.Transform(EnvVarTransformer.Transform);
            GlobalDiagnosticsContext.Set("LogFilePath", pathConfiguration.LogFilePath);

            var configurationProvider = this.Container.Get<IConfigurationProvider>();
            configurationProvider.Initialize(pathConfiguration, Settings.Default.DefaultUserConfiguration);
            var configuration = this.Container.Get<IConfigurationProvider>().Load();

            // Has to be done before the VMs are fetched from the container
            var languageArg = this.Args.FirstOrDefault(x => x.StartsWith("-culture="));
            if (languageArg != null)
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(languageArg.Substring("-culture=".Length));
            else if (!configuration.UseComputerCulture)
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            var autostartProvider = this.Container.Get<IAutostartProvider>();
#if DEBUG
            autostartProvider.IsEnabled = false;
#endif

            if (autostartProvider.CanWrite)
            {
                // If it's not in portable mode, and if we had to create config (i.e. it's the first start ever), then enable autostart
                // Else, keep the config as it was, but update the path to us (if we're not in debug)
                if (Settings.Default.EnableAutostartOnFirstStart && configurationProvider.HadToCreateConfiguration)
                    autostartProvider.SetAutoStart(new AutostartConfiguration() { AutoStart = true, StartMinimized = true });
                else
                    autostartProvider.UpdatePathToSelf();
            }

            var notifyIconManager = this.Container.Get<INotifyIconManager>();
            notifyIconManager.Setup((INotifyIconDelegate)this.RootViewModel);
            this.Container.Get<ConfigurationApplicator>().ApplyConfiguration();

            this.Container.Get<MemoryUsageLogger>().Enabled = true;

            // Horrible workaround for a CefSharp crash on logout/shutdown
            // https://github.com/cefsharp/CefSharp/issues/800#issuecomment-75058534
            this.Application.SessionEnding += (o, e) => Process.GetCurrentProcess().Kill();

            if (configurationProvider.Load().NotifyOfNewVersions)
            {
                SystemEvents.PowerModeChanged += (o, e) =>
                {
                    if (e.Mode == PowerModes.Resume)
                        this.Container.Get<IUpdateChecker>().CheckForAcceptableUpdatesAsync();
                };
            }

            MessageBoxViewModel.ButtonLabels = new Dictionary<MessageBoxResult, string>()
            {
                { MessageBoxResult.Cancel, Localizer.Translate("Generic_Dialog_Cancel") },
                { MessageBoxResult.No, Localizer.Translate("Generic_Dialog_No") },
                { MessageBoxResult.OK, Localizer.Translate("Generic_Dialog_OK") },
                { MessageBoxResult.Yes, Localizer.Translate("Generic_Dialog_Yes") },
            };
        }

        protected override void Launch()
        {
            if (this.Args.Contains("-minimized"))
                this.Container.Get<INotifyIconManager>().EnsureIconVisible();
            else
                base.Launch();
        }

        protected override void OnLaunch()
        {
            var config = this.Container.Get<IConfigurationProvider>().Load();
            if (config.StartSyncthingAutomatically && !this.Args.Contains("-noautostart"))
                ((ShellViewModel)this.RootViewModel).Start();

            // We don't care if this fails
            if (config.NotifyOfNewVersions)
                this.Container.Get<IUpdateChecker>().CheckForAcceptableUpdatesAsync();
        }

        protected override void OnUnhandledException(DispatcherUnhandledExceptionEventArgs e)
        {
            var logger = LogManager.GetCurrentClassLogger();
            logger.Error("An unhandled exception occurred", e.Exception);

            // If we're shutting down, we're not going to be able to display an error dialog....
            // We've logged it. Nothing else we can do.
            if (this.exiting)
                return;

            try
            {
                var windowManager = this.Container.Get<IWindowManager>();

                var configurationException = e.Exception as ConfigurationException;
                if (configurationException != null)
                {
                    windowManager.ShowMessageBox(String.Format("Configuration Error: {0}", configurationException.Message), "Configuration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    e.Handled = true;
                }
                else
                {
                    var vm = this.Container.Get<UnhandledExceptionViewModel>();
                    vm.Exception = e.Exception;
                    windowManager.ShowDialog(vm);
                }
            }
            catch (Exception exception)
            {
                // Don't re-throw. Nasty stuff happens if we throw an exception while trying to handle an unhandled exception
                // For starters, the event log shows the wrong exception - this one, instead of the root cause
                logger.Error("Unhandled exception while trying to display unhandled exception window", exception);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            this.exiting = true;

            // Try and be nice and close SyncTrayzor gracefully, before the Dispose call on SyncThingProcessRunning kills it dead
            this.Container.Get<ISyncThingManager>().StopAsync().Wait(500);
        }
    }
}
