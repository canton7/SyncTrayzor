using NLog;
using Stylet;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SyncTrayzor.Services.UpdateManagement
{
    public class VersionIgnoredEventArgs : EventArgs
    {
        public Version IgnoredVersion { get;  }

        public VersionIgnoredEventArgs(Version ignoredVersion)
        {
            this.IgnoredVersion = ignoredVersion;
        }
    }

    public interface IUpdateManager : IDisposable
    {
        event EventHandler<VersionIgnoredEventArgs> VersionIgnored;
        Version LatestIgnoredVersion { get; set; }
        string UpdateCheckApiUrl { get; set; }
        bool CheckForUpdates { get; set; }

        Task<VersionCheckResults> CheckForAcceptableUpdateAsync();
    }

    public class UpdateManager : IUpdateManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private static readonly TimeSpan deadTimeAfterStarting = TimeSpan.FromMinutes(5);
        // We'll never check more frequently than this, ever
        private static readonly TimeSpan updateCheckDebounceTime = TimeSpan.FromHours(1);
        // If 'remind me later' is active, we'll check this frequently
        private static readonly TimeSpan remindMeLaterTime = TimeSpan.FromDays(3);
        // How often the update checking timer should fire. Having it fire too often is OK: we won't
        // take action
        private static readonly TimeSpan updateCheckingTimerInterval = TimeSpan.FromHours(8);

        private readonly IApplicationState applicationState;
        private readonly IApplicationWindowState applicationWindowState;
        private readonly IUserActivityMonitor userActivityMonitor;
        private readonly IUpdateCheckerFactory updateCheckerFactory;
        private readonly IProcessStartProvider processStartProvider;
        private readonly IUpdatePromptProvider updatePromptProvider;
        private readonly IAssemblyProvider assemblyProvider;
        private readonly Func<IUpdateVariantHandler> updateVariantHandlerFactory;

        private readonly object promptTimerLock = new object();
        private readonly DispatcherTimer promptTimer;

        private readonly SemaphoreSlim versionCheckLock = new SemaphoreSlim(1, 1);

        private DateTime lastCheckedTime;
        private CancellationTokenSource toastCts;
        private bool remindLaterActive;

        public event EventHandler<VersionIgnoredEventArgs> VersionIgnored;
        public Version LatestIgnoredVersion { get; set; }
        public string UpdateCheckApiUrl { get; set; }

        private bool _checkForUpdates;
        public bool CheckForUpdates
        {
            get => this._checkForUpdates;
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
            IApplicationWindowState applicationWindowState,
            IUserActivityMonitor userActivityMonitor,
            IUpdateCheckerFactory updateCheckerFactory,
            IProcessStartProvider processStartProvider,
            IUpdatePromptProvider updatePromptProvider,
            IAssemblyProvider assemblyProvider,
            Func<IUpdateVariantHandler> updateVariantHandlerFactory)
        {
            this.applicationState = applicationState;
            this.applicationWindowState = applicationWindowState;
            this.userActivityMonitor = userActivityMonitor;
            this.updateCheckerFactory = updateCheckerFactory;
            this.processStartProvider = processStartProvider;
            this.updatePromptProvider = updatePromptProvider;
            this.assemblyProvider = assemblyProvider;
            this.updateVariantHandlerFactory = updateVariantHandlerFactory;

            this.promptTimer = new DispatcherTimer();
            this.promptTimer.Tick += this.PromptTimerElapsed;

            // Strategy time:
            // We'll always check when the user starts up or resumes from sleep.
            // We'll check whenever the user opens the app, debounced to a suitable period.
            // We'll check periodically if none of the above have happened, on a longer interval.
            // If 'remind me later' is active, we'll do none of the above for a long interval.

            this.applicationState.ResumeFromSleep += this.ResumeFromSleep;
            this.applicationWindowState.RootWindowActivated += this.RootWindowActivated;
        }

        private bool ShouldCheckForUpdates()
        {
            if (this.remindLaterActive)
                return DateTime.UtcNow - this.lastCheckedTime > remindMeLaterTime;
            else
                return DateTime.UtcNow - this.lastCheckedTime > updateCheckDebounceTime;
        }

        private async void UpdateCheckForUpdates(bool checkForUpdates)
        {
            if (checkForUpdates)
            {
                this.RestartTimer();
                // Give them a minute to catch their breath
                if (this.ShouldCheckForUpdates())
                {
                    await Task.Delay(deadTimeAfterStarting);
                    await this.CheckForUpdatesAsync();
                }
            }
            else
            {
                lock (this.promptTimerLock)
                {
                    this.promptTimer.IsEnabled = false;
                }
            }
        }

        private async void ResumeFromSleep(object sender, EventArgs e)
        {
            if (this.ShouldCheckForUpdates())
            {
                // We often wake up before the network does. Give the network some time to sort itself out
                await Task.Delay(deadTimeAfterStarting);
                await this.CheckForUpdatesAsync();
            }
        }

        private async void RootWindowActivated(object sender, ActivationEventArgs e)
        {
            if (this.toastCts != null)
                this.toastCts.Cancel();

            if (this.ShouldCheckForUpdates())
                await this.CheckForUpdatesAsync();
        }

        private async void PromptTimerElapsed(object sender, EventArgs e)
        {
            if (this.ShouldCheckForUpdates())
                await this.CheckForUpdatesAsync();
        }

        private void OnVersionIgnored(Version ignoredVersion)
        {
            this.VersionIgnored?.Invoke(this, new VersionIgnoredEventArgs(ignoredVersion));
        }

        private void RestartTimer()
        {
            lock(this.promptTimerLock)
            {
                this.promptTimer.IsEnabled = false;
                this.promptTimer.Interval = updateCheckingTimerInterval;
                this.promptTimer.IsEnabled = true;
            }
        }

        private async Task CheckForUpdatesAsync()
        {
            this.RestartTimer();

            if (!this.versionCheckLock.Wait(0))
                return;

            try
            {
                this.lastCheckedTime = DateTime.UtcNow;

                if (!this.CheckForUpdates)
                    return;

                var variantHandler = this.updateVariantHandlerFactory();

                var updateChecker = this.updateCheckerFactory.CreateUpdateChecker(this.UpdateCheckApiUrl, variantHandler.VariantName);
                var checkResult = await updateChecker.CheckForAcceptableUpdateAsync(this.LatestIgnoredVersion);

                if (checkResult == null)
                    return;

                if (!await variantHandler.TryHandleUpdateAvailableAsync(checkResult))
                {
                    logger.Info("Can't update, as TryHandleUpdateAvailableAsync returned false");
                    return;
                }

                VersionPromptResult promptResult;
                if (this.applicationState.HasMainWindow)
                {
                    promptResult = this.updatePromptProvider.ShowDialog(checkResult, variantHandler.CanAutoInstall, variantHandler.RequiresUac);
                }
                else
                {
                    // If another application is fullscreen, don't bother
                    if (this.userActivityMonitor.IsWindowFullscreen())
                    {
                        logger.Debug("Another application was fullscreen, so we didn't prompt the user");
                        return;
                    }

                    try
                    {
                        this.toastCts = new CancellationTokenSource();
                        promptResult = await this.updatePromptProvider.ShowToast(checkResult, variantHandler.CanAutoInstall, variantHandler.RequiresUac, this.toastCts.Token);
                        this.toastCts = null;

                        // Special case
                        if (promptResult == VersionPromptResult.ShowMoreDetails)
                        {
                            this.applicationWindowState.EnsureInForeground();
                            promptResult = this.updatePromptProvider.ShowDialog(checkResult, variantHandler.CanAutoInstall, variantHandler.RequiresUac);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        this.toastCts = null;
                        logger.Debug("Update toast cancelled. Moving to a dialog");
                        promptResult = this.updatePromptProvider.ShowDialog(checkResult, variantHandler.CanAutoInstall, variantHandler.RequiresUac);
                    }
                }

                this.remindLaterActive = false;
                switch (promptResult)
                {
                    case VersionPromptResult.InstallNow:
                        Debug.Assert(variantHandler.CanAutoInstall);
                        logger.Info("Auto-installing {0}", checkResult.NewVersion);
                        variantHandler.AutoInstall(this.PathToRestartApplication());
                        break;

                    case VersionPromptResult.Download:
                        logger.Info("Proceeding to download URL {0}", checkResult.DownloadUrl);
                        this.processStartProvider.StartDetached(checkResult.ReleasePageUrl);
                        break;

                    case VersionPromptResult.Ignore:
                        logger.Info("Ignoring version {0}", checkResult.NewVersion);
                        this.OnVersionIgnored(checkResult.NewVersion);
                        break;

                    case VersionPromptResult.RemindLater:
                        this.remindLaterActive = true;
                        logger.Info("Not installing version {0}, but will remind later", checkResult.NewVersion);
                        break;

                    case VersionPromptResult.ShowMoreDetails:
                    default:
                        Debug.Assert(false);
                        break;
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Error in UpdateManager.CheckForUpdatesAsync");
            }
            finally
            {
                this.versionCheckLock.Release();
            }
        }

        private string PathToRestartApplication()
        {
            var path = $"\"{this.assemblyProvider.Location}\"";
            if (!this.applicationState.HasMainWindow)
                path += " -minimized";

            return path;
        }

        public Task<VersionCheckResults> CheckForAcceptableUpdateAsync()
        {
            var variantHandler = this.updateVariantHandlerFactory();
            var updateChecker = this.updateCheckerFactory.CreateUpdateChecker(this.UpdateCheckApiUrl, variantHandler.VariantName);
            return updateChecker.CheckForAcceptableUpdateAsync(this.LatestIgnoredVersion);
        }

        public void Dispose()
        {
            this.applicationState.ResumeFromSleep -= this.ResumeFromSleep;
            this.applicationWindowState.RootWindowActivated -= this.RootWindowActivated;
        }
    }
}
