using NLog;
using Stylet;
using SyncTrayzor.Pages;
using SyncTrayzor.Services.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace SyncTrayzor.Services.UpdateManagement
{
    public class VersionIgnoredEventArgs : EventArgs
    {
        public Version IgnoredVersion { get; private set; }

        public VersionIgnoredEventArgs(Version ignoredVersion)
        {
            this.IgnoredVersion = ignoredVersion;
        }
    }

    public interface IUpdateManager
    {
        event EventHandler<VersionIgnoredEventArgs> VersionIgnored;
        Version LatestIgnoredVersion { get; set; }
        string Variant { get; set; }
        string UpdateCheckApiUrl { get; set; }
        bool CheckForUpdates { get; set; }
    }

    public class UpdateManager : IUpdateManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly TimeSpan timeBetweenChecks = TimeSpan.FromHours(3);

        private readonly IApplicationState applicationState;
        private readonly IUpdateCheckerFactory updateCheckerFactory;
        private readonly IProcessStartProvider processStartProvider;
        private readonly IUpdatePromptProvider updatePromptProvider;
        private readonly System.Timers.Timer promptTimer;

        private DateTime lastCheckedTime;

        public event EventHandler<VersionIgnoredEventArgs> VersionIgnored;
        public Version LatestIgnoredVersion { get; set; }
        public string Variant { get; set; }
        public string UpdateCheckApiUrl { get; set; }

        private bool _checkForUpdates;
        public bool CheckForUpdates
        {
            get { return this._checkForUpdates; }
            set
            {
                if (this._checkForUpdates == value)
                    return;
                this._checkForUpdates = value;
                this.UpdateCheckForUpdates(value);
            }
        }

        public UpdateManager(
            IApplicationState applicationState,
            IUpdateCheckerFactory updateCheckerFactory,
            IProcessStartProvider processStartProvider,
            IUpdatePromptProvider updatePromptProvider)
        {
            this.applicationState = applicationState;
            this.updateCheckerFactory = updateCheckerFactory;
            this.processStartProvider = processStartProvider;
            this.updatePromptProvider = updatePromptProvider;

            this.promptTimer = new System.Timers.Timer();
            this.promptTimer.Elapsed += this.PromptTimerElapsed;

            // Strategy time:
            // We'll prompt the user a fixed period after the computer starts up / resumes from sleep
            // We'll also check on a fixed interval since this point
            // We'll also check when the application is restored from tray

            this.applicationState.Startup += this.ApplicationStartup;
            this.applicationState.RootWindowActivated += this.RootWindowActivated;
        }

        private async void UpdateCheckForUpdates(bool checkForUpdates)
        {
            if (checkForUpdates)
            {
                this.RestartTimer();
                if (this.UpdateCheckDue())
                    await this.CheckForUpdatesAsync();
            }
            else
            {
                this.promptTimer.Enabled = false;
            }
        }

        private async void ApplicationStartup(object sender, EventArgs e)
        {
            await this.CheckForUpdatesAsync();
        }

        private async void RootWindowActivated(object sender, ActivationEventArgs e)
        {
            if (this.UpdateCheckDue())
                await this.CheckForUpdatesAsync();
        }

        private async void PromptTimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (this.UpdateCheckDue())
                await this.CheckForUpdatesAsync();
        }

        private void OnVersionIgnored(Version ignoredVersion)
        {
            var handler = this.VersionIgnored;
            if (handler != null)
                handler(this, new VersionIgnoredEventArgs(ignoredVersion));
        }

        private bool UpdateCheckDue()
        {
            return DateTime.UtcNow - this.lastCheckedTime > timeBetweenChecks;
        }

        private void RestartTimer()
        {
            this.promptTimer.Enabled = false;
            this.promptTimer.Interval = timeBetweenChecks.TotalMilliseconds;
            this.promptTimer.Enabled = true;
        }

        private async Task CheckForUpdatesAsync()
        {
            this.lastCheckedTime = DateTime.UtcNow;

            if (!this.CheckForUpdates)
                return;

            this.RestartTimer();

            var updateChecker = this.updateCheckerFactory.CreateUpdateChecker(this.UpdateCheckApiUrl, this.Variant);
            var checkResult = await updateChecker.CheckForAcceptableUpdateAsync(this.LatestIgnoredVersion);

            VersionPromptResult promptResult;
            if (this.applicationState.HasMainWindow)
            {
                promptResult = this.updatePromptProvider.ShowDialog(checkResult);
            }
            else
            {
                try
                {
                    promptResult = await this.updatePromptProvider.ShowToast(checkResult, CancellationToken.None);
                }
                catch (OperationCanceledException)
                {
                    logger.Info("Update toast cancelled");
                    return;
                }
            }

            switch (promptResult)
            {
                case VersionPromptResult.Download:
                    logger.Info("Proceeding to download URL {0}", checkResult.DownloadUrl);
                    this.processStartProvider.Start(checkResult.DownloadUrl);
                    break;

                case VersionPromptResult.Ignore:
                    logger.Info("Ignoring version {0}", checkResult.NewVersion);
                    this.OnVersionIgnored(checkResult.NewVersion);
                    break;

                case VersionPromptResult.RemindLater:
                    logger.Info("Not installing version {0}, but will remind later", checkResult.NewVersion);
                    break;

                default:
                    Debug.Assert(false);
                    break;
            }
        }
    }
}
