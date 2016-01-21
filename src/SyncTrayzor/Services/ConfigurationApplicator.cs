using SyncTrayzor.NotifyIcon;
using SyncTrayzor.Properties;
using SyncTrayzor.Services.Config;
using SyncTrayzor.Services.Conflicts;
using SyncTrayzor.Services.UpdateManagement;
using SyncTrayzor.Syncthing;
using SyncTrayzor.Utils;
using System;
using System.Linq;
using SyncTrayzor.Services.Metering;

namespace SyncTrayzor.Services
{
    public class ConfigurationApplicator : IDisposable
    {
        private readonly IConfigurationProvider configurationProvider;

        private readonly IApplicationPathsProvider pathsProvider;
        private readonly INotifyIconManager notifyIconManager;
        private readonly ISyncthingManager syncthingManager;
        private readonly IAutostartProvider autostartProvider;
        private readonly IWatchedFolderMonitor watchedFolderMonitor;
        private readonly IUpdateManager updateManager;
        private readonly IConflictFileWatcher conflictFileWatcher;
        private readonly IAlertsManager alertsManager;
        private readonly IMeteredNetworkManager meteredNetworkManager;

        public ConfigurationApplicator(
            IConfigurationProvider configurationProvider,
            IApplicationPathsProvider pathsProvider,
            INotifyIconManager notifyIconManager,
            ISyncthingManager syncthingManager,
            IAutostartProvider autostartProvider,
            IWatchedFolderMonitor watchedFolderMonitor,
            IUpdateManager updateManager,
            IConflictFileWatcher conflictFileWatcher,
            IAlertsManager alertsManager,
            IMeteredNetworkManager meteredNetworkManager)
        {
            this.configurationProvider = configurationProvider;
            this.configurationProvider.ConfigurationChanged += this.ConfigurationChanged;

            this.pathsProvider = pathsProvider;
            this.notifyIconManager = notifyIconManager;
            this.syncthingManager = syncthingManager;
            this.autostartProvider = autostartProvider;
            this.watchedFolderMonitor = watchedFolderMonitor;
            this.updateManager = updateManager;
            this.conflictFileWatcher = conflictFileWatcher;
            this.alertsManager = alertsManager;
            this.meteredNetworkManager = meteredNetworkManager;

            this.syncthingManager.DataLoaded += this.OnDataLoaded;
            this.updateManager.VersionIgnored += this.VersionIgnored;
        }

        private void ConfigurationChanged(object sender, ConfigurationChangedEventArgs e)
        {
            this.ApplyNewConfiguration(e.NewConfiguration);
        }

        private void VersionIgnored(object sender, VersionIgnoredEventArgs e)
        {
            this.configurationProvider.AtomicLoadAndSave(config => config.LatestNotifiedVersion = e.IgnoredVersion);
        }

        public void ApplyConfiguration()
        {
            this.watchedFolderMonitor.BackoffInterval = TimeSpan.FromMilliseconds(Settings.Default.DirectoryWatcherBackoffMilliseconds);
            this.watchedFolderMonitor.FolderExistenceCheckingInterval = TimeSpan.FromMilliseconds(Settings.Default.DirectoryWatcherFolderExistenceCheckMilliseconds);

            this.conflictFileWatcher.BackoffInterval = TimeSpan.FromMilliseconds(Settings.Default.DirectoryWatcherBackoffMilliseconds);
            this.conflictFileWatcher.FolderExistenceCheckingInterval = TimeSpan.FromMilliseconds(Settings.Default.DirectoryWatcherFolderExistenceCheckMilliseconds);

            this.syncthingManager.SyncthingConnectTimeout = TimeSpan.FromSeconds(Settings.Default.SyncthingConnectTimeoutSeconds);

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

            this.syncthingManager.PreferredAddress = new Uri("https://" + configuration.SyncthingAddress);
            this.syncthingManager.ApiKey = configuration.SyncthingApiKey;
            this.syncthingManager.SyncthingCommandLineFlags = configuration.SyncthingCommandLineFlags;
            this.syncthingManager.SyncthingEnvironmentalVariables = configuration.SyncthingEnvironmentalVariables;
            this.syncthingManager.SyncthingCustomHomeDir = configuration.SyncthingUseCustomHome ?
                EnvVarTransformer.Transform(configuration.SyncthingCustomHomePath)
                : null;
            this.syncthingManager.SyncthingDenyUpgrade = configuration.SyncthingDenyUpgrade;
            this.syncthingManager.SyncthingPriorityLevel = configuration.SyncthingPriorityLevel;
            this.syncthingManager.SyncthingHideDeviceIds = configuration.ObfuscateDeviceIDs;
            this.syncthingManager.ExecutablePath = EnvVarTransformer.Transform(configuration.SyncthingPath);
            this.syncthingManager.DebugFacilities.SetEnabledDebugFacilities(configuration.SyncthingDebugFacilities);

            this.watchedFolderMonitor.WatchedFolderIDs = configuration.Folders.Where(x => x.IsWatched).Select(x => x.ID);

            this.updateManager.LatestIgnoredVersion = configuration.LatestNotifiedVersion;
            this.updateManager.CheckForUpdates = configuration.NotifyOfNewVersions;

            this.conflictFileWatcher.IsEnabled = configuration.EnableConflictFileMonitoring;

            this.meteredNetworkManager.IsEnabled = configuration.PauseDevicesOnMeteredNetworks;

            this.alertsManager.EnableConflictedFileAlerts = configuration.EnableConflictFileMonitoring;
            this.alertsManager.EnableFailedTransferAlerts = configuration.EnableFailedTransferAlerts;
        }

        private void OnDataLoaded(object sender, EventArgs e)
        {
            this.configurationProvider.AtomicLoadAndSave(c =>
            {
                this.LoadFolders(c);
            });
        }

        private void LoadFolders(Configuration configuration)
        {
            var folderIds = this.syncthingManager.Folders.FetchAll().Select(x => x.FolderId).ToList();

            foreach (var newKey in folderIds.Except(configuration.Folders.Select(x => x.ID)))
            {
                configuration.Folders.Add(new FolderConfiguration(newKey, true, true));
            }

            configuration.Folders = configuration.Folders.Where(x => folderIds.Contains(x.ID)).ToList();
        }

        public void Dispose()
        {
            this.configurationProvider.ConfigurationChanged -= this.ConfigurationChanged;
            this.syncthingManager.DataLoaded -= this.OnDataLoaded;
            this.updateManager.VersionIgnored -= this.VersionIgnored;
        }
    }
}
