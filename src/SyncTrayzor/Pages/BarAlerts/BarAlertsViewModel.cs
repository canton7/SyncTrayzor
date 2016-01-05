using Stylet;
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

        public BarAlertsViewModel(IAlertsManager alertsManager)
        {
            this.alertsManager = alertsManager;

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
                this.Items.Add(new ConflictsAlertViewModel(conflictedFilesCount));
        }

        public void Dispose()
        {
            this.alertsManager.AlertsStateChanged -= this.AlertsStateChanged;
        }
    }
}
