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
using SyncTrayzor.SyncThing.TransferHistory;
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
        private bool startMinimized;

        protected override void OnStart()
        {
            this.startMinimized = this.Args.Contains("-minimized");
        }

        protected override void ConfigureIoC(IStyletIoCBuilder builder)
        {
            builder.Bind<IApplicationState>().ToInstance(new ApplicationState(this.Application));
            builder.Bind<IApplicationWindowState>().To<ApplicationWindowState>().InSingletonScope();
            builder.Bind<IConfigurationProvider>().To<ConfigurationProvider>().InSingletonScope();
            builder.Bind<IApplicationPathsProvider>().To<ApplicationPathsProvider>().InSingletonScope();
            builder.Bind<IAssemblyProvider>().To<AssemblyProvider>().InSingletonScope();
            builder.Bind<IAutostartProvider>().To<AutostartProvider>().InSingletonScope();
            builder.Bind<ConfigurationApplicator>().ToSelf().InSingletonScope();
            builder.Bind<ISyncThingApiClientFactory>().To<SyncThingApiClientFactory>();
            builder.Bind<ISyncThingEventWatcherFactory>().To<SyncThingEventWatcherFactory>();
            builder.Bind<ISyncThingProcessRunner>().To<SyncThingProcessRunner>().InSingletonScope();
            builder.Bind<ISyncThingManager>().To<SyncThingManager>().InSingletonScope();
            builder.Bind<ISyncThingConnectionsWatcherFactory>().To<SyncThingConnectionsWatcherFactory>();
            builder.Bind<INotifyIconManager>().To<NotifyIconManager>().InSingletonScope();
            builder.Bind<IWatchedFolderMonitor>().To<WatchedFolderMonitor>().InSingletonScope();
            builder.Bind<IUpdateManager>().To<UpdateManager>().InSingletonScope();
            builder.Bind<IUpdateDownloader>().To<UpdateDownloader>().InSingletonScope();
            builder.Bind<IUpdateCheckerFactory>().To<UpdateCheckerFactory>();
            builder.Bind<IUpdatePromptProvider>().To<UpdatePromptProvider>();
            builder.Bind<IUpdateNotificationClientFactory>().To<UpdateNotificationClientFactory>();
            builder.Bind<IInstallerCertificateVerifier>().To<InstallerCertificateVerifier>().InSingletonScope();
            builder.Bind<IProcessStartProvider>().To<ProcessStartProvider>().InSingletonScope();
            builder.Bind<IFilesystemProvider>().To<FilesystemProvider>().InSingletonScope();

            if (Settings.Default.Variant == SyncTrayzorVariant.Installed)
                builder.Bind<IUpdateVariantHandler>().To<InstalledUpdateVariantHandler>();
            else if (Settings.Default.Variant == SyncTrayzorVariant.Portable)
                builder.Bind<IUpdateVariantHandler>().To<PortableUpdateVariantHandler>();
            else
                Trace.Assert(false);

            builder.Bind(typeof(IModelValidator<>)).To(typeof(FluentModelValidator<>));
            builder.Bind(typeof(IValidator<>)).ToAllImplementations(this.Assemblies);
        }

        protected override void Configure()
        {
            var pathConfiguration = Settings.Default.PathConfiguration;
            GlobalDiagnosticsContext.Set("LogFilePath", EnvVarTransformer.Transform(pathConfiguration.LogFilePath));

            this.Container.Get<IApplicationPathsProvider>().Initialize(pathConfiguration);

            var configurationProvider = this.Container.Get<IConfigurationProvider>();
            configurationProvider.Initialize(Settings.Default.DefaultUserConfiguration);
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

            // If it's not in portable mode, and if we had to create config (i.e. it's the first start ever), then enable autostart
            if (autostartProvider.CanWrite && Settings.Default.EnableAutostartOnFirstStart && configurationProvider.HadToCreateConfiguration)
                    autostartProvider.SetAutoStart(new AutostartConfiguration() { AutoStart = true, StartMinimized = true });

            // Needs to be done before ConfigurationApplicator is run
            this.Container.Get<IApplicationWindowState>().Setup((ShellViewModel)this.RootViewModel);

            this.Container.Get<ConfigurationApplicator>().ApplyConfiguration();

            this.Container.Get<MemoryUsageLogger>().Enabled = true;

            // Horrible workaround for a CefSharp crash on logout/shutdown
            // https://github.com/cefsharp/CefSharp/issues/800#issuecomment-75058534
            // Also handles Restart Manager requests - sent by the installer. We need to shutdown syncthing and Cef in this case
            this.Application.SessionEnding += (o, e) =>
            {
                var manager = this.Container.Get<ISyncThingManager>();
                manager.StopAsync().Wait(250);
                Task.Delay(250).Wait();
                manager.Kill();

                Process.GetCurrentProcess().Kill();
            };

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
            if (this.startMinimized)
                this.Container.Get<INotifyIconManager>().EnsureIconVisible();
            else
                base.Launch();
        }

        protected override void OnLaunch()
        {
            this.Container.Get<IApplicationState>().ApplicationStarted();

            var config = this.Container.Get<IConfigurationProvider>().Load();
            if (config.StartSyncthingAutomatically && !this.Args.Contains("-noautostart"))
                ((ShellViewModel)this.RootViewModel).Start();

            // If we've just been upgraded, and we're minimized, show a bit of toast explaining the fact
            if (this.startMinimized && this.Container.Get<IConfigurationProvider>().WasUpgraded)
            {
                var updatedVm = this.Container.Get<NewVersionInstalledToastViewModel>();
                updatedVm.Version = this.Container.Get<IAssemblyProvider>().Version;
                this.Container.Get<INotifyIconManager>().ShowBalloonAsync(updatedVm, timeout: 5000); 
            }
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
