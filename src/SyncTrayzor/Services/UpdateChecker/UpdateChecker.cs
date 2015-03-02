using NLog;
using Stylet;
using SyncTrayzor.Pages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SyncTrayzor.Services.UpdateChecker
{
    public class VersionIgnoredEventArgs : EventArgs
    {
        public Version IgnoredVersion { get; private set; }

        public VersionIgnoredEventArgs(Version ignoredVersion)
        {
            this.IgnoredVersion = ignoredVersion;
        }
    }

    public class VersionCheckResults
    {
        public Version LatestVersion { get; private set; }
        public bool LatestVersionIsNewer { get; private set; }
        public string LatestVersionDownloadUrl { get; private set; }
        public string LatestVersionChangelog { get; private set; }

        public VersionCheckResults(Version latestVersion, bool latestVersionIsNewer, string latestVersionDownloadUrl, string latestVersionChangelog)
        {
            this.LatestVersion = latestVersion;
            this.LatestVersionIsNewer = latestVersionIsNewer;
            this.LatestVersionDownloadUrl = latestVersionDownloadUrl;
            this.LatestVersionChangelog = latestVersionChangelog;
        }

        public override string ToString()
        {
            return String.Format("<VersionCheckResults LatestVersion={0} IsNewer={1} DownloadUrl={2}>", this.LatestVersion, this.LatestVersionIsNewer, this.LatestVersionDownloadUrl);
        }
    }

    public interface IUpdateChecker
    {
        event EventHandler<VersionIgnoredEventArgs> VersionIgnored;
        Version LatestIgnoredVersion { get; set; }

        Task<VersionCheckResults> FetchUpdatesAsync();
        Task CheckForUpdatesAsync();
    }

    public class UpdateChecker : IUpdateChecker
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IWindowManager windowManager;
        private readonly IGithubApiClient apiClient;
        private readonly Func<NewVersionAlertViewModel> newersionAlertViewModelFactory;

        public Version LatestIgnoredVersion { get; set; }
        public event EventHandler<VersionIgnoredEventArgs> VersionIgnored;

        public UpdateChecker(
            IWindowManager windowManager,
            IGithubApiClient apiClient,
            Func<NewVersionAlertViewModel> newVersionAlertViewModelFactory)
        {
            this.windowManager = windowManager;
            this.apiClient = apiClient;
            this.newersionAlertViewModelFactory = newVersionAlertViewModelFactory;
        }

        public async Task<VersionCheckResults> FetchUpdatesAsync()
        {
            // We don't care if we fail
            try
            {
                var applicationVersion = Assembly.GetExecutingAssembly().GetName().Version;
                var latestRelease = await this.apiClient.FetchLatestReleaseAsync();

                if (latestRelease == null)
                {
                    logger.Info("No suitable releases found");
                    return null;
                }

                var results = new VersionCheckResults(latestRelease.Version, latestRelease.Version > applicationVersion, latestRelease.DownloadUrl, latestRelease.Body);
                logger.Info("Found new version: {0}", results);
                return results;
            }
            catch (Exception e)
            {
                logger.Warn("Fetching updates failed with an error", e);
                return null;
            }
        }
        
        public async Task CheckForUpdatesAsync()
        {
            var results = await this.FetchUpdatesAsync();

            if (results == null)
                return;

            if (this.LatestIgnoredVersion != null && results.LatestVersion <= this.LatestIgnoredVersion)
                return;

            if (results.LatestVersionIsNewer)
            {
                var vm = this.newersionAlertViewModelFactory();
                vm.Changelog = results.LatestVersionChangelog;
                vm.Version = results.LatestVersion;
                var result = this.windowManager.ShowDialog(vm);
                if (result == true)
                {
                    logger.Info("Proceeding to download URL {0}", results.LatestVersionDownloadUrl);
                    Process.Start(results.LatestVersionDownloadUrl);
                }
                else
                {
                    logger.Info("Ignoring version {0}", results.LatestVersion);
                    this.OnVersionIgnored(results.LatestVersion);
                }
            }
        }

        private void OnVersionIgnored(Version ignoredVersion)
        {
            var handler = this.VersionIgnored;
            if (handler != null)
                handler(this, new VersionIgnoredEventArgs(ignoredVersion));
        }
    }
}
