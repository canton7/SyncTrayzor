using FluentValidation;
using Stylet;
using SyncTrayzor.Properties;
using SyncTrayzor.Services;
using SyncTrayzor.Services.Config;
using SyncTrayzor.Syncthing;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using System.IO;
using SyncTrayzor.Services.Metering;

namespace SyncTrayzor.Pages.Settings
{
    public class FolderSettings : PropertyChangedBase
    {
        public string FolderName { get; set; }
        public bool IsWatched { get; set; }
        public bool IsNotified { get; set; }
    }

    public class DebugFacilitySetting : PropertyChangedBase
    {
        public bool IsEnabled { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class SettingsViewModel : Screen
    {
        // We can be opened directly on this tab. All of the layout is done in xaml, so this is
        // the neatest way we can select it...
        private const int loggingTabIndex = 3;

        private readonly IConfigurationProvider configurationProvider;
        private readonly IAutostartProvider autostartProvider;
        private readonly IWindowManager windowManager;
        private readonly IProcessStartProvider processStartProvider;
        private readonly IAssemblyProvider assemblyProvider;
        private readonly IApplicationState applicationState;
        private readonly IApplicationPathsProvider applicationPathsProvider;
        private readonly ISyncthingManager syncthingManager;
        private readonly List<SettingItem> settings = new List<SettingItem>();

        public int SelectedTabIndex { get; set; }

        public SettingItem<bool> MinimizeToTray { get; }
        public SettingItem<bool> CloseToTray { get; }
        public SettingItem<bool> NotifyOfNewVersions { get; }
        public SettingItem<bool> ObfuscateDeviceIDs { get; }
        public SettingItem<bool> UseComputerCulture { get; }
        public SettingItem<bool> DisableHardwareRendering { get; }
        public SettingItem<bool> EnableConflictFileMonitoring { get; }
        public SettingItem<bool> EnableFailedTransferAlerts { get; }

        public bool PauseDevicesOnMeteredNetworksSupported { get; }
        public SettingItem<bool> PauseDevicesOnMeteredNetworks { get; }

        public SettingItem<bool> ShowTrayIconOnlyOnClose { get; }
        public SettingItem<bool> ShowSynchronizedBalloonEvenIfNothingDownloaded { get; }
        public SettingItem<bool> ShowDeviceConnectivityBalloons { get; }

        public SettingItem<bool> StartSyncthingAutomatically { get; }

        public BindableCollection<LabelledValue<SyncthingPriorityLevel>> PriorityLevels { get; }
        public SettingItem<SyncthingPriorityLevel> SyncthingPriorityLevel { get; }

        public SettingItem<bool> SyncthingUseDefaultHome { get; }
        public SettingItem<string> SyncthingAddress { get; }
        public SettingItem<string> SyncthingApiKey { get; }

        public bool CanReadAutostart { get; set; }
        public bool CanWriteAutostart { get; set; }
        public bool CanReadOrWriteAutostart => this.CanReadAutostart || this.CanWriteAutostart; 
        public bool CanReadAndWriteAutostart => this.CanReadAutostart && this.CanWriteAutostart;
        public bool StartOnLogon { get; set; }
        public bool StartMinimized { get; set; }
        public bool StartMinimizedEnabled => this.CanReadAndWriteAutostart && this.StartOnLogon;
        public SettingItem<string> SyncthingCommandLineFlags { get; }
        public SettingItem<string> SyncthingEnvironmentalVariables { get; }
        public SettingItem<bool> SyncthingDenyUpgrade { get;  }

        private bool updatingFolderSettings;
        public bool? AreAllFoldersWatched { get; set; }
        public bool? AreAllFoldersNotified { get; set; }
        public BindableCollection<FolderSettings> FolderSettings { get; } = new BindableCollection<FolderSettings>();

        public BindableCollection<DebugFacilitySetting> SyncthingDebugFacilities { get; } = new BindableCollection<DebugFacilitySetting>();

        public SettingsViewModel(
            IConfigurationProvider configurationProvider,
            IAutostartProvider autostartProvider,
            IWindowManager windowManager,
            IProcessStartProvider processStartProvider,
            IAssemblyProvider assemblyProvider,
            IApplicationState applicationState,
            IApplicationPathsProvider applicationPathsProvider,
            ISyncthingManager syncthingManager,
            IMeteredNetworkManager meteredNetworkManager)
        {
            this.configurationProvider = configurationProvider;
            this.autostartProvider = autostartProvider;
            this.windowManager = windowManager;
            this.processStartProvider = processStartProvider;
            this.assemblyProvider = assemblyProvider;
            this.applicationState = applicationState;
            this.applicationPathsProvider = applicationPathsProvider;
            this.syncthingManager = syncthingManager;

            this.MinimizeToTray = this.CreateBasicSettingItem(x => x.MinimizeToTray);
            this.NotifyOfNewVersions = this.CreateBasicSettingItem(x => x.NotifyOfNewVersions);
            this.CloseToTray = this.CreateBasicSettingItem(x => x.CloseToTray);
            this.ObfuscateDeviceIDs = this.CreateBasicSettingItem(x => x.ObfuscateDeviceIDs);
            this.UseComputerCulture = this.CreateBasicSettingItem(x => x.UseComputerCulture);
            this.UseComputerCulture.RequiresSyncTrayzorRestart = true;
            this.DisableHardwareRendering = this.CreateBasicSettingItem(x => x.DisableHardwareRendering);
            this.DisableHardwareRendering.RequiresSyncTrayzorRestart = true;
            this.EnableConflictFileMonitoring = this.CreateBasicSettingItem(x => x.EnableConflictFileMonitoring);
            this.EnableFailedTransferAlerts = this.CreateBasicSettingItem(x => x.EnableFailedTransferAlerts);

            this.PauseDevicesOnMeteredNetworks = this.CreateBasicSettingItem(x => x.PauseDevicesOnMeteredNetworks);
            this.PauseDevicesOnMeteredNetworksSupported = meteredNetworkManager.IsSupportedByWindows;

            this.ShowTrayIconOnlyOnClose = this.CreateBasicSettingItem(x => x.ShowTrayIconOnlyOnClose);
            this.ShowSynchronizedBalloonEvenIfNothingDownloaded = this.CreateBasicSettingItem(x => x.ShowSynchronizedBalloonEvenIfNothingDownloaded);
            this.ShowDeviceConnectivityBalloons = this.CreateBasicSettingItem(x => x.ShowDeviceConnectivityBalloons);

            this.StartSyncthingAutomatically = this.CreateBasicSettingItem(x => x.StartSyncthingAutomatically);
            this.SyncthingPriorityLevel = this.CreateBasicSettingItem(x => x.SyncthingPriorityLevel);
            this.SyncthingPriorityLevel.RequiresSyncthingRestart = true;
            this.SyncthingUseDefaultHome = this.CreateBasicSettingItem(x => !x.SyncthingUseCustomHome, (x, v) => x.SyncthingUseCustomHome = !v);
            this.SyncthingUseDefaultHome.RequiresSyncthingRestart = true;
            this.SyncthingAddress = this.CreateBasicSettingItem(x => x.SyncthingAddress, new SyncthingAddressValidator());
            this.SyncthingAddress.RequiresSyncthingRestart = true;
            this.SyncthingApiKey = this.CreateBasicSettingItem(x => x.SyncthingApiKey, new SyncthingApiKeyValidator());
            this.SyncthingApiKey.RequiresSyncthingRestart = true;

            this.CanReadAutostart = this.autostartProvider.CanRead;
            this.CanWriteAutostart = this.autostartProvider.CanWrite;
            if (this.autostartProvider.CanRead)
            {
                var currentSetup = this.autostartProvider.GetCurrentSetup();
                this.StartOnLogon = currentSetup.AutoStart;
                this.StartMinimized = currentSetup.StartMinimized;
            }

            this.SyncthingCommandLineFlags = this.CreateBasicSettingItem(
                x => String.Join(" ", x.SyncthingCommandLineFlags),
                (x, v) =>
                {
                    IEnumerable<KeyValuePair<string, string>> envVars;
                    KeyValueStringParser.TryParse(v, out envVars, mustHaveValue: false);
                    x.SyncthingCommandLineFlags = envVars.Select(item => KeyValueStringParser.FormatItem(item.Key, item.Value)).ToList();
                }, new SyncthingCommandLineFlagsValidator());
            this.SyncthingCommandLineFlags.RequiresSyncthingRestart = true;

            this.SyncthingEnvironmentalVariables = this.CreateBasicSettingItem(
                x => KeyValueStringParser.Format(x.SyncthingEnvironmentalVariables),
                (x, v) =>
                {
                    IEnumerable<KeyValuePair<string, string>> envVars;
                    KeyValueStringParser.TryParse(v, out envVars);
                    x.SyncthingEnvironmentalVariables = new EnvironmentalVariableCollection(envVars);
                }, new SyncthingEnvironmentalVariablesValidator());
            this.SyncthingEnvironmentalVariables.RequiresSyncthingRestart = true;

            this.SyncthingDenyUpgrade = this.CreateBasicSettingItem(x => x.SyncthingDenyUpgrade);
            this.SyncthingDenyUpgrade.RequiresSyncthingRestart = true;

            var configuration = this.configurationProvider.Load();

            foreach (var settingItem in this.settings)
            {
                settingItem.LoadValue(configuration);
            }

            foreach (var folderSetting in this.FolderSettings)
            {
                folderSetting.Bind(s => s.IsWatched, (o, e) => this.UpdateAreAllFoldersWatched());
                folderSetting.Bind(s => s.IsNotified, (o, e) => this.UpdateAreAllFoldersNotified());
            }

            this.PriorityLevels = new BindableCollection<LabelledValue<SyncthingPriorityLevel>>()
            {
                LabelledValue.Create(Resources.SettingsView_Syncthing_ProcessPriority_AboveNormal, SyncTrayzor.Services.Config.SyncthingPriorityLevel.AboveNormal),
                LabelledValue.Create(Resources.SettingsView_Syncthing_ProcessPriority_Normal, SyncTrayzor.Services.Config.SyncthingPriorityLevel.Normal),
                LabelledValue.Create(Resources.SettingsView_Syncthing_ProcessPriority_BelowNormal, SyncTrayzor.Services.Config.SyncthingPriorityLevel.BelowNormal),
                LabelledValue.Create(Resources.SettingsView_Syncthing_ProcessPriority_Idle, SyncTrayzor.Services.Config.SyncthingPriorityLevel.Idle),
            };

            this.Bind(s => s.AreAllFoldersNotified, (o, e) =>
            {
                if (this.updatingFolderSettings)
                    return;

                this.updatingFolderSettings = true;

                foreach (var folderSetting in this.FolderSettings)
                {
                    folderSetting.IsNotified = e.NewValue.GetValueOrDefault(false);
                }

                this.updatingFolderSettings = false;
            });

            this.Bind(s => s.AreAllFoldersWatched, (o, e) =>
            {
                if (this.updatingFolderSettings)
                    return;

                this.updatingFolderSettings = true;

                foreach (var folderSetting in this.FolderSettings)
                {
                    folderSetting.IsWatched = e.NewValue.GetValueOrDefault(false);
                }

                this.updatingFolderSettings = false;
            });

            this.UpdateAreAllFoldersWatched();
            this.UpdateAreAllFoldersNotified();
        }

        protected override void OnInitialActivate()
        {
            if (syncthingManager.State == SyncthingState.Running && syncthingManager.IsDataLoaded)
                this.LoadFromSyncthingStartupData();
            else
                this.syncthingManager.DataLoaded += this.SyncthingDataLoaded;
        }

        protected override void OnClose()
        {
            this.syncthingManager.DataLoaded -= this.SyncthingDataLoaded;
        }

        private void SyncthingDataLoaded(object sender, EventArgs e)
        {
            this.LoadFromSyncthingStartupData();
        }

        private void LoadFromSyncthingStartupData()
        {
            var configuration = this.configurationProvider.Load();

            this.FolderSettings.Clear();
            this.FolderSettings.AddRange(configuration.Folders.OrderByDescending(x => x.ID).Select(x => new FolderSettings()
            {
                FolderName = x.ID,
                IsWatched = x.IsWatched,
                IsNotified = x.NotificationsEnabled,
            }));
            this.NotifyOfPropertyChange(nameof(this.FolderSettings));

            this.SyncthingDebugFacilities.Clear();
            this.SyncthingDebugFacilities.AddRange(syncthingManager.DebugFacilities.DebugFacilities.Select(x => new DebugFacilitySetting()
            {
                IsEnabled = x.IsEnabled,
                Name = x.Name,
                Description = x.Description,
            }));
            this.NotifyOfPropertyChange(nameof(this.SyncthingDebugFacilities));
        }

        private SettingItem<T> CreateBasicSettingItem<T>(Expression<Func<Configuration, T>> accessExpression, IValidator<SettingItem<T>> validator = null)
        {
            return this.CreateBasicSettingItemImpl(v => new SettingItem<T>(accessExpression, v), validator);
        }

        private SettingItem<T> CreateBasicSettingItem<T>(Func<Configuration, T> getter, Action<Configuration, T> setter, IValidator<SettingItem<T>> validator = null, Func<T, T, bool> comparer = null)
        {
            return this.CreateBasicSettingItemImpl(v => new SettingItem<T>(getter, setter, v, comparer), validator);
        }

        private SettingItem<T> CreateBasicSettingItemImpl<T>(Func<IModelValidator, SettingItem<T>> generator, IValidator<SettingItem<T>> validator)
        {
            IModelValidator modelValidator = validator == null ? null : new FluentModelValidator<SettingItem<T>>(validator);
            var settingItem = generator(modelValidator);
            this.settings.Add(settingItem);
            settingItem.ErrorsChanged += (o, e) => this.NotifyOfPropertyChange(() => this.CanSave);
            return settingItem;
        }

        private void UpdateAreAllFoldersWatched()
        {
            if (this.updatingFolderSettings)
                return;

            this.updatingFolderSettings = true;

            if (this.FolderSettings.All(x => x.IsWatched))
                this.AreAllFoldersWatched = true;
            else if (this.FolderSettings.All(x => !x.IsWatched))
                this.AreAllFoldersWatched = false;
            else
                this.AreAllFoldersWatched = null;

            this.updatingFolderSettings = false;
        }

        private void UpdateAreAllFoldersNotified()
        {
            if (this.updatingFolderSettings)
                return;

            this.updatingFolderSettings = true;

            if (this.FolderSettings.All(x => x.IsNotified))
                this.AreAllFoldersNotified = true;
            else if (this.FolderSettings.All(x => !x.IsNotified))
                this.AreAllFoldersNotified = false;
            else
                this.AreAllFoldersNotified = null;

            this.updatingFolderSettings = false;
        }

        public bool CanSave => this.settings.All(x => !x.HasErrors);
        public void Save()
        {
            bool debugFacilitiesRequiresRestart = !this.syncthingManager.DebugFacilities.SupportsRestartlessUpdate &&
                !new HashSet<string>(this.syncthingManager.DebugFacilities.DebugFacilities.Where(x => x.IsEnabled).Select(x => x.Name)).SetEquals(this.SyncthingDebugFacilities.Where(x => x.IsEnabled).Select(x => x.Name));

            this.configurationProvider.AtomicLoadAndSave(configuration =>
            {
                foreach (var settingItem in this.settings)
                {
                    settingItem.SaveValue(configuration);
                }

                configuration.Folders = this.FolderSettings.Select(x => new FolderConfiguration(x.FolderName, x.IsWatched, x.IsNotified)).ToList();
                // The ConfigurationApplicator will propagate this to the DebugFacilitiesManager
                configuration.SyncthingDebugFacilities = this.SyncthingDebugFacilities.Where(x => x.IsEnabled).Select(x => x.Name).ToList();
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
            else if ((this.settings.Any(x => x.HasChanged && x.RequiresSyncthingRestart) || debugFacilitiesRequiresRestart) &&
                this.syncthingManager.State == SyncthingState.Running)
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
            await this.syncthingManager.RestartAsync();
        }

        public void Cancel()
        {
            this.RequestClose(false);
        }

        public void ShowSyncthingLogFile()
        {
            this.processStartProvider.ShowInExplorer(Path.Combine(this.applicationPathsProvider.LogFilePath, "syncthing.log"));
        }

        public void ShowSyncTrayzorLogFile()
        {
            this.processStartProvider.ShowInExplorer(Path.Combine(this.applicationPathsProvider.LogFilePath, "SyncTrayzor.log"));
        }

        public void SelectLoggingTab()
        {
            this.SelectedTabIndex = loggingTabIndex;
        }
    }
}
