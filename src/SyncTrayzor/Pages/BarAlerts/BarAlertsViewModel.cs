using Stylet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages.BarAlerts
{
    public class BarAlertsViewModel : Conductor<IBarAlert>.Collection.AllActive
    {
        public BarAlertsViewModel()
        {
            this.Items.Add(new ConflictsAlertViewModel());
        }
    }
}
