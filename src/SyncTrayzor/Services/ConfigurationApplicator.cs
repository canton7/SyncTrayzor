using SyncTrayzor.NotifyIcon;
using SyncTrayzor.Properties;
using SyncTrayzor.Services.Config;
using SyncTrayzor.Services.UpdateManagement;
using SyncTrayzor.SyncThing;
using SyncTrayzor.Utils;
using System;
using System.Linq;
using System.Collections.Generic;

namespace SyncTrayzor.Services
{
    public class ConfigurationApplicator
    {
        private readonly IConfigurationProvider configurationProvider;

        private readonly IApplicationPathsProvider pathsProvider;
        private readonly INotifyIconManager notifyIconManager;
        private readonly ISyncThingManager syncThingManager;
        private readonly IAutostartProvider autostartProvider;
        private readonly IWatchedFolderMonitor watchedFolderMonitor;
        private readonly IUpdateManager updateManager;

        public ConfigurationApplicator(
            IConfigurationProvider configurationProvider,
            IApplicationPathsProvider pathsProvider,
            INotifyIconManager notifyIconManager,
            ISyncThingManager syncThingManager,
            IAutostartProvider autostartProvider,
            IWatchedFolderMonitor watchedFolderMonitor,
            IUpdateManager updateManager)
        {
            this.configurationProvider = configurationProvider;
            this.configurationProvider.ConfigurationChanged += (o, e) => this.ApplyNewConfiguration(e.NewConfiguration);

            this.pathsProvider = pathsProvider;
            this.notifyIconManager = notifyIconManager;
            this.syncThingManager = syncThingManager;
            this.autostartProvider = autostartProvider;
            this.watchedFolderMonitor = watchedFolderMonitor;
            this.updateManager = updateManager;

            this.syncThingManager.DataLoaded += (o, e) => this.OnDataLoaded();
            this.updateManager.VersionIgnored += (o, e) => this.configurationProvider.AtomicLoadAndSave(config => config.LatestNotifiedVersion = e.IgnoredVersion);
        }

        public void ApplyConfiguration()
        {
            this.watchedFolderMonitor.BackoffInterval = TimeSpan.FromMilliseconds(Settings.Default.DirectoryWatcherBackoffMilliseconds);
            this.watchedFolderMonitor.FolderExistenceCheckingInterval = TimeSpan.FromMilliseconds(Settings.Default.DirectoryWatcherFolderExistenceCheckMilliseconds);

            this.syncThingManager.SyncthingConnectTimeout = TimeSpan.FromSeconds(Settings.Default.SyncthingConnectTimeoutSeconds);

            this.updateManager.UpdateCheckApiUrl = Settings.Default.UpdateApiUrl;
            this.updateManager.UpdateCheckInterval = TimeSpan.FromSeconds(Settings.Default.UpdateCheckIntervalSeconds);

            this.ApplyNewConfiguration(this.configurationProvider.Load());
        }

        private void ApplyNewConfiguration(Configuration configuration)
        {
            this.notifyIconManager.MinimizeToTray = configuration.MinimizeToTray;
            this.notifyIconManager.CloseToTray = configuration.CloseToTray;
            this.notifyIconManager.ShowOnlyOnClose = configuration.ShowTrayIconOnlyOnClose;
            this.notifyIconManager.FolderNotificationsEnabled = configuration.Folders.ToDictionary(x => x.ID, x => x.NotificationsEnabled);
            this.notifyIconManager.ShowSynchronizedBalloonEvenIfNothingDownloaded = configuration.ShowSynchronizedBalloonEvenIfNothingDownloaded;
            this.notifyIconManager.ShowDeviceConnectivityBalloons = configuration.ShowDeviceConnectivityBalloons;

            this.syncThingManager.PreferredAddress = new Uri("https://" + configuration.SyncthingAddress);
            this.syncThingManager.ApiKey = configuration.SyncthingApiKey;
            this.syncThingManager.SyncthingCommandLineFlags = configuration.SyncthingCommandLineFlags;
            this.syncThingManager.SyncthingEnvironmentalVariables = configuration.SyncthingEnvironmentalVariables;
            this.syncThingManager.SyncthingCustomHomeDir = configuration.SyncthingUseCustomHome ?
                EnvVarTransformer.Transform(configuration.SyncthingCustomHomePath)
                : null;
            this.syncThingManager.SyncthingDenyUpgrade = configuration.SyncthingDenyUpgrade;
            this.syncThingManager.SyncthingPriorityLevel = configuration.SyncthingPriorityLevel;
            this.syncThingManager.SyncthingHideDeviceIds = configuration.ObfuscateDeviceIDs;
            this.syncThingManager.ExecutablePath = EnvVarTransformer.Transform(configuration.SyncthingPath);
            this.syncThingManager.DebugFacilities.SetEnabledDebugFacilities(configuration.SyncthingDebugFacilities);

            this.watchedFolderMonitor.WatchedFolderIDs = configuration.Folders.Where(x => x.IsWatched).Select(x => x.ID);

            this.updateManager.LatestIgnoredVersion = configuration.LatestNotifiedVersion;
            this.updateManager.CheckForUpdates = configuration.NotifyOfNewVersions;
        }

        private void OnDataLoaded()
        {
            this.configurationProvider.AtomicLoadAndSave(c =>
            {
                this.LoadFolders(c);
            });
        }

        private void LoadFolders(Configuration configuration)
        {
            var folderIds = this.syncThingManager.Folders.FetchAll().Select(x => x.FolderId).ToList();

            foreach (var newKey in folderIds.Except(configuration.Folders.Select(x => x.ID)))
            {
                configuration.Folders.Add(new FolderConfiguration(newKey, true, true));
            }

            configuration.Folders = configuration.Folders.Where(x => folderIds.Contains(x.ID)).ToList();
        }
    }
}
