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

namespace SyncTrayzor.Services.UpdateManagement
{
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
        Task<VersionCheckResults> FetchUpdatesAsync();
        Task<VersionCheckResults> CheckForAcceptableUpdatesAsync(Version latestIgnoredVersion = null);
    }

    public class UpdateChecker : IUpdateChecker
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IGithubApiClient apiClient;

        public UpdateChecker(IGithubApiClient apiClient)
        {
            this.apiClient = apiClient;
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
        
        public async Task<VersionCheckResults> CheckForAcceptableUpdatesAsync(Version latestIgnoredVersion)
        {
            var results = await this.FetchUpdatesAsync();

            if (results == null)
                return null;

            if (latestIgnoredVersion != null && results.LatestVersion <= latestIgnoredVersion)
                return null;

            if (!results.LatestVersionIsNewer)
                return null;

            return results;
        }
    }
}
