using FluentValidation;
using Stylet;
using SyncTrayzor.Properties.Strings;
using SyncTrayzor.Services;
using SyncTrayzor.Services.Config;
using SyncTrayzor.SyncThing;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;

namespace SyncTrayzor.Pages.Settings
{
    public class WatchedFolder
    {
        public string Folder { get; set; }
        public bool IsSelected { get; set; }
    }

    public class SettingsViewModel : Screen
    {
        private readonly IConfigurationProvider configurationProvider;
        private readonly IAutostartProvider autostartProvider;
        private readonly IWindowManager windowManager;
        private readonly IProcessStartProvider processStartProvider;
        private readonly IAssemblyProvider assemblyProvider;
        private readonly IApplicationState applicationState;
        private readonly ISyncThingManager syncThingManager;
        private readonly List<SettingItem> settings = new List<SettingItem>();

        public SettingItem<bool> MinimizeToTray { get; set; }
        public SettingItem<bool> CloseToTray { get; set; }
        public SettingItem<bool> NotifyOfNewVersions { get; set; }
        public SettingItem<bool> ObfuscateDeviceIDs { get; set; }
        public SettingItem<bool> UseComputerCulture { get; set; }
        public SettingItem<bool> DisableHardwareRendering { get; set; }

        public SettingItem<bool> ShowTrayIconOnlyOnClose { get; set; }
        public SettingItem<bool> ShowSynchronizedBalloon { get; set; }
        public SettingItem<bool> ShowSynchronizedBalloonEvenIfNothingDownloaded { get; set; }
        public SettingItem<bool> ShowDeviceConnectivityBalloons { get; set; }

        public SettingItem<bool> StartSyncThingAutomatically { get; set; }
        public SettingItem<bool> SyncthingRunLowPriority { get; set; }
        public SettingItem<bool> SyncthingUseDefaultHome { get; set; }
        public SettingItem<string> SyncThingAddress { get; set; }
        public SettingItem<string> SyncThingApiKey { get; set; }

        public bool CanReadAutostart { get; set; }
        public bool CanWriteAutostart { get; set; }
        public bool CanReadOrWriteAutostart
        {
            get { return this.CanReadAutostart || this.CanWriteAutostart; }
        }
        public bool CanReadAndWriteAutostart
        {
            get { return this.CanReadAutostart && this.CanWriteAutostart; }
        }
        public bool StartOnLogon { get; set; }
        public bool StartMinimized { get; set; }
        public bool StartMinimizedEnabled
        {
            get { return this.CanReadAndWriteAutostart && this.StartOnLogon; }
        }

        public BindableCollection<WatchedFolder> WatchedFolders { get; set; }

        public SettingItem<string> SyncThingEnvironmentalVariables { get; set; }
        public SettingItem<bool> SyncthingDenyUpgrade { get; set; }

        public SettingsViewModel(
            IConfigurationProvider configurationProvider,
            IAutostartProvider autostartProvider,
            IWindowManager windowManager,
            IProcessStartProvider processStartProvider,
            IAssemblyProvider assemblyProvider,
            IApplicationState applicationState,
            ISyncThingManager syncThingManager)
        {
            this.configurationProvider = configurationProvider;
            this.autostartProvider = autostartProvider;
            this.windowManager = windowManager;
            this.processStartProvider = processStartProvider;
            this.assemblyProvider = assemblyProvider;
            this.applicationState = applicationState;
            this.syncThingManager = syncThingManager;

            this.MinimizeToTray = this.CreateBasicSettingItem(x => x.MinimizeToTray);
            this.NotifyOfNewVersions = this.CreateBasicSettingItem(x => x.NotifyOfNewVersions);
            this.CloseToTray = this.CreateBasicSettingItem(x => x.CloseToTray);
            this.ObfuscateDeviceIDs = this.CreateBasicSettingItem(x => x.ObfuscateDeviceIDs);
            this.UseComputerCulture = this.CreateBasicSettingItem(x => x.UseComputerCulture);
            this.UseComputerCulture.RequiresSyncTrayzorRestart = true;
            this.DisableHardwareRendering = this.CreateBasicSettingItem(x => x.DisableHardwareRendering);
            this.DisableHardwareRendering.RequiresSyncTrayzorRestart = true;

            this.ShowTrayIconOnlyOnClose = this.CreateBasicSettingItem(x => x.ShowTrayIconOnlyOnClose);
            this.ShowSynchronizedBalloon = this.CreateBasicSettingItem(x => x.ShowSynchronizedBalloon);
            this.ShowSynchronizedBalloonEvenIfNothingDownloaded = this.CreateBasicSettingItem(x => x.ShowSynchronizedBalloonEvenIfNothingDownloaded);
            this.ShowDeviceConnectivityBalloons = this.CreateBasicSettingItem(x => x.ShowDeviceConnectivityBalloons);

            this.StartSyncThingAutomatically = this.CreateBasicSettingItem(x => x.StartSyncthingAutomatically);
            this.SyncthingRunLowPriority = this.CreateBasicSettingItem(x => x.SyncthingRunLowPriority);
            this.SyncthingRunLowPriority.RequiresSyncthingRestart = true;
            this.SyncthingUseDefaultHome = this.CreateBasicSettingItem(x => !x.SyncthingUseCustomHome, (x, v) => x.SyncthingUseCustomHome = !v);
            this.SyncthingUseDefaultHome.RequiresSyncthingRestart = true;
            this.SyncThingAddress = this.CreateBasicSettingItem(x => x.SyncthingAddress, new SyncThingAddressValidator());
            this.SyncThingAddress.RequiresSyncthingRestart = true;
            this.SyncThingApiKey = this.CreateBasicSettingItem(x => x.SyncthingApiKey, new SyncThingApiKeyValidator());
            this.SyncThingApiKey.RequiresSyncthingRestart = true;

            this.CanReadAutostart = this.autostartProvider.CanRead;
            this.CanWriteAutostart = this.autostartProvider.CanWrite;
            if (this.autostartProvider.CanRead)
            {
                var currentSetup = this.autostartProvider.GetCurrentSetup();
                this.StartOnLogon = currentSetup.AutoStart;
                this.StartMinimized = currentSetup.StartMinimized;
            }

            this.SyncThingEnvironmentalVariables = this.CreateBasicSettingItem(
                x => EnvironmentalVariablesParser.Format(x.SyncthingEnvironmentalVariables),
                (x, v) =>
                {
                    EnvironmentalVariableCollection envVars;
                    EnvironmentalVariablesParser.TryParse(v, out envVars);
                    x.SyncthingEnvironmentalVariables = envVars;    
                }, new SyncThingEnvironmentalVariablesValidator());
            this.SyncThingEnvironmentalVariables.RequiresSyncthingRestart = true;

            this.SyncthingDenyUpgrade = this.CreateBasicSettingItem(x => x.SyncthingDenyUpgrade);
            this.SyncthingDenyUpgrade.RequiresSyncthingRestart = true;

            var configuration = this.configurationProvider.Load();

            foreach (var settingItem in this.settings)
            {
                settingItem.LoadValue(configuration);
            }

            this.WatchedFolders = new BindableCollection<WatchedFolder>(configuration.Folders.Select(x => new WatchedFolder()
            {
                Folder = x.ID,
                IsSelected = x.IsWatched
            }));
        }

        private SettingItem<T> CreateBasicSettingItem<T>(Expression<Func<Configuration, T>> accessExpression, IValidator<SettingItem<T>> validator = null)
        {
            return this.CreateBasicSettingItemImpl(v => new SettingItem<T>(accessExpression, v), validator);
        }

        private SettingItem<T> CreateBasicSettingItem<T>(Func<Configuration, T> getter, Action<Configuration, T> setter, IValidator<SettingItem<T>> validator = null)
        {
            return this.CreateBasicSettingItemImpl(v => new SettingItem<T>(getter, setter, v), validator);
        }

        private SettingItem<T> CreateBasicSettingItemImpl<T>(Func<IModelValidator, SettingItem<T>> generator, IValidator<SettingItem<T>> validator)
        {
            IModelValidator modelValidator = validator == null ? null : new FluentModelValidator<SettingItem<T>>(validator);
            var settingItem = generator(modelValidator);
            this.settings.Add(settingItem);
            settingItem.ErrorsChanged += (o, e) => this.NotifyOfPropertyChange(() => this.CanSave);
            return settingItem;
        }

        public bool CanSave
        {
            get { return this.settings.All(x => !x.HasErrors); }
        }
        public void Save()
        {
            this.configurationProvider.AtomicLoadAndSave(configuration =>
            {
                foreach (var settingItem in this.settings)
                {
                    settingItem.SaveValue(configuration);
                }

                configuration.Folders = this.WatchedFolders.Select(x => new FolderConfiguration(x.Folder, x.IsSelected)).ToList();
            });

            if (this.autostartProvider.CanWrite)
            {
                var autostartConfig = new AutostartConfiguration() { AutoStart = this.StartOnLogon, StartMinimized = this.StartMinimized };
                this.autostartProvider.SetAutoStart(autostartConfig);
            }

            if (this.settings.Any(x => x.HasChanged && x.RequiresSyncTrayzorRestart))
            {
                var result = this.windowManager.ShowMessageBox(
                    Resources.SettingsView_RestartSyncTrayzor_Message,
                    Resources.SettingsView_RestartSyncTrayzor_Title,
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    this.processStartProvider.StartDetached(this.assemblyProvider.Location);
                    this.applicationState.Shutdown();
                }
            }
            else if (this.settings.Any(x => x.HasChanged && x.RequiresSyncthingRestart) && this.syncThingManager.State == SyncThingState.Running)
            {
                var result = this.windowManager.ShowMessageBox(
                    Resources.SettingsView_RestartSyncthing_Message,
                    Resources.SettingsView_RestartSyncthing_Title,
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    this.RestartSyncthing();
                }
            }

            this.RequestClose(true);
        }

        private async void RestartSyncthing()
        {
            await this.syncThingManager.RestartAsync();
        }

        public void Cancel()
        {
            this.RequestClose(false);
        }
    }
}
