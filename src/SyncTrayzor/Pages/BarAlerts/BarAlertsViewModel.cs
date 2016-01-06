using Stylet;
using SyncTrayzor.Pages.ConflictResolution;
using SyncTrayzor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages.BarAlerts
{
    public class BarAlertsViewModel : Conductor<IBarAlert>.Collection.AllActive, IDisposable
    {
        private readonly IAlertsManager alertsManager;
        private readonly Func<ConflictResolutionViewModel> conflictResolutionViewModelFactory;
        private readonly IWindowManager windowManager;

        public BarAlertsViewModel(
            IAlertsManager alertsManager,
            Func<ConflictResolutionViewModel> conflictResolutionViewModelFactory,
            IWindowManager windowManager)
        {
            this.alertsManager = alertsManager;
            this.conflictResolutionViewModelFactory = conflictResolutionViewModelFactory;
            this.windowManager = windowManager;

            this.alertsManager.AlertsStateChanged += this.AlertsStateChanged;
        }

        private void AlertsStateChanged(object sender, EventArgs e)
        {
            this.Items.Clear();

            var conflictedFilesCount = this.alertsManager.ConflictedFiles.Count;
            if (conflictedFilesCount > 0)
            {
                var vm = new ConflictsAlertViewModel(conflictedFilesCount);
                vm.OpenConflictResolverClicked += (o, e2) => this.OpenConflictResolver();
                this.Items.Add(vm);
            }

            var foldersWithFailedTransferFiles = this.alertsManager.FoldersWithFailedTransferFiles;
            if (foldersWithFailedTransferFiles.Count > 0)
            {
                var vm = new FailedTransfersAlertViewModel(foldersWithFailedTransferFiles);
                this.Items.Add(vm);
            }
        }

        private void OpenConflictResolver()
        {
            var vm = this.conflictResolutionViewModelFactory();
            this.windowManager.ShowDialog(vm);
        }

        public void Dispose()
        {
            this.alertsManager.AlertsStateChanged -= this.AlertsStateChanged;
        }
    }
}
