using Stylet;
using SyncTrayzor.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.NotifyIcon
{
    public class UpgradeAvailableViewModel : Screen
    {
        public NewVersionAlertViewModel Content2 { get; private set; }

        public UpgradeAvailableViewModel(NewVersionAlertViewModel newVersionAlertViewModel)
        {
            this.Content2 = newVersionAlertViewModel;
            this.Content2.ConductWith(this);
        }
    }
}
