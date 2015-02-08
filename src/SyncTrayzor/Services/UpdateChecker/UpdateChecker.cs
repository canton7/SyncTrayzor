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

    public interface IUpdateChecker
    {
        event EventHandler<VersionIgnoredEventArgs> VersionIgnored;
        Version LatestIgnoredVersion { get; set; }
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

        public async Task CheckForUpdatesAsync()
        {
            var applicationVersion = Assembly.GetExecutingAssembly().GetName().Version;

            var latestRelease = await this.apiClient.FetchLatestReleaseAsync();

            if (this.LatestIgnoredVersion != null && latestRelease.Version <= this.LatestIgnoredVersion)
                return;

            if (latestRelease.Version > applicationVersion)
            {
                var msg = String.Format("A new version of SyncTrayzor is available! Do you want to download version {0}?", latestRelease.Version);
                var result = this.windowManager.ShowMessageBox(msg, "Upgrade Version?", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                    Process.Start(latestRelease.DownloadUrl);
                else
                    this.OnVersionIgnored(latestRelease.Version);
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
