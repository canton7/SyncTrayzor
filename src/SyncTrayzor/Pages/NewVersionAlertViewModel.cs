using Stylet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages
{
    public class NewVersionAlertViewModel : Screen
    {
        public Version Version { get; set; }
        public string Changelog { get; set; }

        public bool DontRemindMe { get; private set; }

        public NewVersionAlertViewModel()
        {
            this.DisplayName = "SyncTrayzor update available";
        }

        public void Download()
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
    }
}
