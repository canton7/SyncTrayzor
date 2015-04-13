using Stylet;
using SyncTrayzor.NotifyIcon;
using SyncTrayzor.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.UpdateManagement
{
    public enum VersionPromptResult
    {
        InstallNow,
        Download,
        RemindLater,
        Ignore,
        ShowMoreDetails,
    }

    public interface IUpdatePromptProvider
    {
        VersionPromptResult ShowDialog(VersionCheckResults checkResults, bool canAutoInstall);
        Task<VersionPromptResult> ShowToast(VersionCheckResults checkResults, bool canAutoInstall, CancellationToken cancellationToken);
    }

    public class UpdatePromptProvider : IUpdatePromptProvider
    {
        private readonly IWindowManager windowManager;
        private readonly Func<NewVersionAlertViewModel> newVersionAlertViewModelFactory;
        private readonly INotifyIconManager notifyIconManager;
        private readonly Func<NewVersionAlertToastViewModel> upgradeAvailableToastViewModelFactory;

        public UpdatePromptProvider(
            IWindowManager windowManager,
            Func<NewVersionAlertViewModel> newVersionAlertViewModelFactory,
            INotifyIconManager notifyIconManager,
            Func<NewVersionAlertToastViewModel> upgradeAvailableToastViewModelFactory)
        {
            this.windowManager = windowManager;
            this.newVersionAlertViewModelFactory = newVersionAlertViewModelFactory;
            this.notifyIconManager = notifyIconManager;
            this.upgradeAvailableToastViewModelFactory = upgradeAvailableToastViewModelFactory;
        }

        public VersionPromptResult ShowDialog(VersionCheckResults checkResults, bool canAutoInstall)
        {
            var vm = this.newVersionAlertViewModelFactory();
            vm.Changelog = checkResults.ReleaseNotes;
            vm.Version = checkResults.NewVersion;
            vm.CanInstall = canAutoInstall;
            var dialogResult = this.windowManager.ShowDialog(vm);

            if (dialogResult == true)
                return canAutoInstall ? VersionPromptResult.InstallNow : VersionPromptResult.Download;
            if (vm.DontRemindMe)
                return VersionPromptResult.Ignore;
            return VersionPromptResult.RemindLater;
        }

        public async Task<VersionPromptResult> ShowToast(VersionCheckResults checkResults, bool canAutoInstall, CancellationToken cancellationToken)
        {
            var vm = this.upgradeAvailableToastViewModelFactory();
            vm.Version = checkResults.NewVersion;
            vm.CanInstall = canAutoInstall;

            var dialogResult = await this.notifyIconManager.ShowBalloonAsync(vm, cancellationToken);

            if (dialogResult == true)
                return canAutoInstall ? VersionPromptResult.InstallNow : VersionPromptResult.Download;
            if (vm.ShowMoreDetails)
                return VersionPromptResult.ShowMoreDetails;
            if (vm.DontRemindMe)
                return VersionPromptResult.Ignore;
            return VersionPromptResult.RemindLater;
        }
    }
}
