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
            foreach (var vm in this.Items.OfType<ConflictsAlertViewModel>().ToList())
            {
                this.Items.Remove(vm);
            }

            var conflictedFilesCount = this.alertsManager.ConflictedFiles.Count;
            if (conflictedFilesCount > 0)
            {
                var vm = new ConflictsAlertViewModel(conflictedFilesCount);
                vm.OpenConflictResolverClicked += (o, e2) => this.OpenConflictResolver();
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
