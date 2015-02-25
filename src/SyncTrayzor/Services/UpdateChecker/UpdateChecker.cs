using Stylet;
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

        public VersionCheckResults(Version latestVersion, bool latestVersionIsNewer, string latestVersionDownloadUrl)
        {
            this.LatestVersion = latestVersion;
            this.LatestVersionIsNewer = latestVersionIsNewer;
            this.LatestVersionDownloadUrl = latestVersionDownloadUrl;
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
        private readonly IWindowManager windowManager;
        private readonly IGithubApiClient apiClient;

        public Version LatestIgnoredVersion { get; set; }
        public event EventHandler<VersionIgnoredEventArgs> VersionIgnored;

        public UpdateChecker(IWindowManager windowManager, IGithubApiClient apiClient)
        {
            this.windowManager = windowManager;
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
                    return null;

                return new VersionCheckResults(latestRelease.Version, latestRelease.Version > applicationVersion, latestRelease.DownloadUrl);
            }
            catch (Exception)
            {
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
                var msg = String.Format("A new version of SyncTrayzor is available! Do you want to download version {0}?\n\nYou will not be prompted for this version again.", results.LatestVersion);
                var result = this.windowManager.ShowMessageBox(msg, "Upgrade Version?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                    Process.Start(results.LatestVersionDownloadUrl);
                else
                    this.OnVersionIgnored(results.LatestVersion);
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
