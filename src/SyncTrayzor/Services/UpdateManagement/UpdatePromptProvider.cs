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
        Download,
        RemindLater,
        Ignore
    }

    public interface IUpdatePromptProvider
    {
        VersionPromptResult ShowDialog(VersionCheckResults checkResults);
        Task<VersionPromptResult> ShowToast(VersionCheckResults checkResults, CancellationToken cancellationToken);
    }

    public class UpdatePromptProvider : IUpdatePromptProvider
    {
        private readonly IWindowManager windowManager;
        private readonly Func<NewVersionAlertViewModel> newVersionAlertViewModelFactory;
        private readonly INotifyIconManager notifyIconManager;
        private readonly Func<UpgradeAvailableToastViewModel> upgradeAvailableToastViewModelFactory;

        public UpdatePromptProvider(
            IWindowManager windowManager,
            Func<NewVersionAlertViewModel> newVersionAlertViewModelFactory,
            INotifyIconManager notifyIconManager,
            Func<UpgradeAvailableToastViewModel> upgradeAvailableToastViewModelFactory)
        {
            this.windowManager = windowManager;
            this.newVersionAlertViewModelFactory = newVersionAlertViewModelFactory;
            this.notifyIconManager = notifyIconManager;
            this.upgradeAvailableToastViewModelFactory = upgradeAvailableToastViewModelFactory;
        }

        public VersionPromptResult ShowDialog(VersionCheckResults checkResults)
        {
            var vm = this.newVersionAlertViewModelFactory();
            vm.Changelog = checkResults.ReleaseNotes;
            vm.Version = checkResults.NewVersion;
            var dialogResult = this.windowManager.ShowDialog(vm);

            if (dialogResult == true)
                return VersionPromptResult.Download;
            if (vm.DontRemindMe)
                return VersionPromptResult.Ignore;
            return VersionPromptResult.RemindLater;
        }

        public async Task<VersionPromptResult> ShowToast(VersionCheckResults checkResults, CancellationToken cancellationToken)
        {
            var vm = this.upgradeAvailableToastViewModelFactory();
            vm.Changelog = checkResults.ReleaseNotes;
            vm.Version = checkResults.NewVersion;

            var tcs = new TaskCompletionSource<VersionPromptResult>();

            vm.DownloadNowClicked += (o, e) => tcs.SetResult(VersionPromptResult.Download);
            vm.IgnoreClicked += (o, e) => tcs.SetResult(VersionPromptResult.Ignore);
            vm.RemindMeLaterClicked += (o, e) => tcs.SetResult(VersionPromptResult.RemindLater);

            using (cancellationToken.Register(() =>
            {
                this.notifyIconManager.HideBalloon();
                tcs.SetCanceled();
            }))
            {
                this.notifyIconManager.ShowBalloon(vm);
                return await tcs.Task;
            }
        }
    }
}
