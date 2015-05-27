using SyncTrayzor.NotifyIcon;
using SyncTrayzor.Properties;
using SyncTrayzor.Services.Config;
using SyncTrayzor.Services.UpdateManagement;
using SyncTrayzor.SyncThing;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            this.syncThingManager.DataLoaded += (o, e) => this.LoadFolders();
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
            this.notifyIconManager.ShowSynchronizedBalloon = configuration.ShowSynchronizedBalloon;
            this.notifyIconManager.ShowDeviceConnectivityBalloons = configuration.ShowDeviceConnectivityBalloons;

            this.syncThingManager.Address = new Uri("https://" + configuration.SyncthingAddress);
            this.syncThingManager.ApiKey = configuration.SyncthingApiKey;
            this.syncThingManager.SyncthingEnvironmentalVariables = configuration.SyncthingEnvironmentalVariables;
            this.syncThingManager.SyncthingCustomHomeDir = configuration.SyncthingUseCustomHome ?
                EnvVarTransformer.Transform(configuration.SyncthingCustomHomePath)
                : null;
            this.syncThingManager.SyncthingDenyUpgrade = configuration.SyncthingDenyUpgrade;
            this.syncThingManager.SyncthingRunLowPriority = configuration.SyncthingRunLowPriority;
            this.syncThingManager.SyncthingHideDeviceIds = configuration.ObfuscateDeviceIDs;
            this.syncThingManager.ExecutablePath = EnvVarTransformer.Transform(configuration.SyncthingPath);

            this.watchedFolderMonitor.WatchedFolderIDs = configuration.Folders.Where(x => x.IsWatched).Select(x => x.ID);

            this.updateManager.LatestIgnoredVersion = configuration.LatestNotifiedVersion;
            this.updateManager.CheckForUpdates = configuration.NotifyOfNewVersions;
        }

        private void LoadFolders()
        {
            var configuration = this.configurationProvider.Load();
            var folderIds = this.syncThingManager.Folders.FetchAll().Select(x => x.FolderId).ToList();

            foreach (var newKey in folderIds.Except(configuration.Folders.Select(x => x.ID)))
            {
                configuration.Folders.Add(new FolderConfiguration(newKey, true));
            }

            configuration.Folders = configuration.Folders.Where(x => folderIds.Contains(x.ID)).ToList();

            this.configurationProvider.Save(configuration);
        }
    }
}
