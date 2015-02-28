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

        public NewVersionAlertViewModel()
        {
            this.DisplayName = "New Version Available";
        }

        public void Download()
        {
            this.RequestClose(true);
        }

        public void Close()
        {
            this.RequestClose(false);
        }
    }
}
