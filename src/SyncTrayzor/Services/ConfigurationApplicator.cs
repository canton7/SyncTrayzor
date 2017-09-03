using SyncTrayzor.NotifyIcon;
using SyncTrayzor.Services.Config;
using SyncTrayzor.Services.Conflicts;
using SyncTrayzor.Services.UpdateManagement;
using SyncTrayzor.Syncthing;
using System;
using System.Linq;
using SyncTrayzor.Services.Metering;
using System.Collections.Generic;

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
        private readonly IPathTransformer pathTransformer;

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
            IMeteredNetworkManager meteredNetworkManager,
            IPathTransformer pathTransformer)
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
            this.pathTransformer = pathTransformer;

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
            this.watchedFolderMonitor.BackoffInterval = TimeSpan.FromMilliseconds(AppSettings.Instance.DirectoryWatcherBackoffMilliseconds);
            this.watchedFolderMonitor.FolderExistenceCheckingInterval = TimeSpan.FromMilliseconds(AppSettings.Instance.DirectoryWatcherFolderExistenceCheckMilliseconds);

            this.conflictFileWatcher.BackoffInterval = TimeSpan.FromMilliseconds(AppSettings.Instance.DirectoryWatcherBackoffMilliseconds);
            this.conflictFileWatcher.FolderExistenceCheckingInterval = TimeSpan.FromMilliseconds(AppSettings.Instance.DirectoryWatcherFolderExistenceCheckMilliseconds);

            this.syncthingManager.SyncthingConnectTimeout = TimeSpan.FromSeconds(AppSettings.Instance.SyncthingConnectTimeoutSeconds);

            this.updateManager.UpdateCheckApiUrl = AppSettings.Instance.UpdateApiUrl;

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
            this.notifyIconManager.ShowDeviceOrFolderRejectedBalloons = configuration.ShowDeviceOrFolderRejectedBalloons;

            this.syncthingManager.PreferredHostAndPort = configuration.SyncthingAddress;
            this.syncthingManager.SyncthingCommandLineFlags = configuration.SyncthingCommandLineFlags;
            this.syncthingManager.SyncthingEnvironmentalVariables = configuration.SyncthingEnvironmentalVariables;
            this.syncthingManager.SyncthingCustomHomeDir = String.IsNullOrWhiteSpace(configuration.SyncthingCustomHomePath) ?
                this.pathsProvider.DefaultSyncthingHomePath :
                this.pathTransformer.MakeAbsolute(configuration.SyncthingCustomHomePath);
            this.syncthingManager.SyncthingDenyUpgrade = configuration.SyncthingDenyUpgrade;
            this.syncthingManager.SyncthingPriorityLevel = configuration.SyncthingPriorityLevel;
            this.syncthingManager.SyncthingHideDeviceIds = configuration.ObfuscateDeviceIDs;
            this.syncthingManager.ExecutablePath = String.IsNullOrWhiteSpace(configuration.SyncthingCustomPath) ?
                this.pathsProvider.DefaultSyncthingPath :
                this.pathTransformer.MakeAbsolute(configuration.SyncthingCustomPath);
            this.syncthingManager.DebugFacilities.SetEnabledDebugFacilities(configuration.SyncthingDebugFacilities);

            this.watchedFolderMonitor.WatchedFolderIDs = configuration.Folders.Where(x => x.IsWatched).Select(x => x.ID);

            this.updateManager.LatestIgnoredVersion = configuration.LatestNotifiedVersion;
            this.updateManager.CheckForUpdates = configuration.NotifyOfNewVersions;

            this.conflictFileWatcher.IsEnabled = configuration.EnableConflictFileMonitoring;

            this.meteredNetworkManager.IsEnabled = configuration.PauseDevicesOnMeteredNetworks;

            this.alertsManager.EnableConflictedFileAlerts = configuration.EnableConflictFileMonitoring;
            this.alertsManager.EnableFailedTransferAlerts = configuration.EnableFailedTransferAlerts;

            SetLogLevel(configuration);
        }

        private static readonly Dictionary<LogLevel, NLog.LogLevel> logLevelMapping = new Dictionary<Config.LogLevel, NLog.LogLevel>()
        {
            { LogLevel.Info, NLog.LogLevel.Info },
            { LogLevel.Debug, NLog.LogLevel.Debug },
            { LogLevel.Trace, NLog.LogLevel.Trace },
        };

        private static void SetLogLevel(Configuration configuration)
        {
            var logLevel = logLevelMapping[configuration.LogLevel];
            var rules = NLog.LogManager.Configuration.LoggingRules;
            var logFileRule = rules.FirstOrDefault(rule => rule.Targets.Any(target => target.Name == "logfile"));
            if (logFileRule != null)
            {
                foreach (var level in NLog.LogLevel.AllLoggingLevels)
                {
                    if (level < logLevel)
                        logFileRule.DisableLoggingForLevel(level);
                    else
                        logFileRule.EnableLoggingForLevel(level);
                }
                NLog.LogManager.ReconfigExistingLoggers();
            }
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

            // If all folders are not watched, new folders are not watched too. Likewise notifications.
            bool areAnyWatched = configuration.Folders.Any(x => x.IsWatched);
            bool areAnyNotifications = configuration.Folders.Any(x => x.NotificationsEnabled);

            foreach (var newKey in folderIds.Except(configuration.Folders.Select(x => x.ID)))
            {
                configuration.Folders.Add(new FolderConfiguration(newKey, areAnyWatched, areAnyNotifications));
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
