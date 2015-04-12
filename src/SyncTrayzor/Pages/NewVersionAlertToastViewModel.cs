using Stylet;
using SyncTrayzor.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages
{
    public class NewVersionAlertToastViewModel : Screen
    {
        public bool CanInstall { get; set; }
        public Version Version { get; set; }

        public bool DontRemindMe { get; private set; }
        public bool ShowMoreDetails { get; private set; }

        public NewVersionAlertToastViewModel()
        {
        }

        public void Download()
        {
            this.RequestClose(true);
        }

        public void Install()
        {
            this.RequestClose(true);
        }

        public void RemindLater()
        {
            this.RequestClose(false);
        }

        public void DontRemind()
        {
            this.DontRemindMe = true;
            this.RequestClose(false);
        }

        public void DisplayMoreDetails()
        {
            this.ShowMoreDetails = true;
            this.RequestClose(false);
        }
    }
}
