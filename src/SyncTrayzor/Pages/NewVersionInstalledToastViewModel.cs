using Stylet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages
{
    public class NewVersionInstalledToastViewModel : Screen
    {
        public Version Version { get; set; }
        public string VersionString
        {
            get { return this.Version.ToString(3); }
        }
    }
}
