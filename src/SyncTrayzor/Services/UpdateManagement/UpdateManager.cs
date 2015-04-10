using NLog;
using Stylet;
using SyncTrayzor.Pages;
using SyncTrayzor.Services.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SyncTrayzor.Services.UpdateManagement
{
    public interface IUpdateManager
    {
        Version LatestIgnoredVersion { get; set; }
    }

    public class UpdateManager : IUpdateManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IWindowManager windowManager;
        private readonly IConfigurationProvider configurationProvider;
        private readonly IUpdateChecker updateChecker;
        private readonly IProcessStartProvider processStartProvider;
        private readonly Func<NewVersionAlertViewModel> newVersionAlertViewModelFactory;
        private readonly Timer promptTimer;

        private Version latestNotifiedVersion;
        private DateTime LastNotififedTime;

        public Version LatestIgnoredVersion { get; set; }

        public UpdateManager(
            IWindowManager windowManager,
            IConfigurationProvider configurationProvider,
            IUpdateChecker updateChecker,
            IProcessStartProvider processStartProvider,
            Func<NewVersionAlertViewModel> newVersionAlertViewModelFactory)
        {
            this.windowManager = windowManager;
            this.configurationProvider = configurationProvider;
            this.updateChecker = updateChecker;
            this.processStartProvider = processStartProvider;
            this.newVersionAlertViewModelFactory = newVersionAlertViewModelFactory;

            this.promptTimer = new Timer();
            this.promptTimer.Elapsed += this.PromptTimerElapsed;

            // Strategy time:
            // We'll prompt the user a fixed period after the computer starts up / resumes from sleep
            // We'll also check on a fixed interval since this point
            // We'll also check when the application is restored from tray
        }

        private void PromptTimerElapsed(object sender, ElapsedEventArgs e)
        {

        }

        private void UpdateIgnoredVersion(Version ignoredVersion)
        {
            this.configurationProvider.AtomicLoadAndSave(configuration => configuration.LatestNotifiedVersion = ignoredVersion);
        }

        private async Task CheckForUpdatesAsync()
        {
            var latestVersion = this.configurationProvider.Load().LatestNotifiedVersion;
            var checkResult = await this.updateChecker.CheckForAcceptableUpdatesAsync(latestVersion);

            var vm = this.newVersionAlertViewModelFactory();
            vm.Changelog = checkResult.LatestVersionChangelog;
            vm.Version = checkResult.LatestVersion;
            var result = this.windowManager.ShowDialog(vm);
            if (result == true)
            {
                logger.Info("Proceeding to download URL {0}", checkResult.LatestVersionDownloadUrl);
                this.processStartProvider.Start(checkResult.LatestVersionDownloadUrl);
            }
            else if (vm.DontRemindMe)
            {
                logger.Info("Ignoring version {0}", checkResult.LatestVersion);
                this.UpdateIgnoredVersion(checkResult.LatestVersion);
            }
            else
            {
                logger.Info("Not installing version {0}, but will remind later", checkResult.LatestVersion);
            }
        }
    }
}
