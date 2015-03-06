using Stylet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages
{
    public class ThirdPartyComponentsViewModel : Conductor<ThirdPartyComponent>.Collection.OneActive
    {
        public ThirdPartyComponentsViewModel()
        {
            this.ActivateItem(new ThirdPartyComponent()
            {
                Name = "Syncthing",
                Description = "Syncthing is distributed as a binary, and is hosted by SyncTrayzor.",
            });
        }
    }

    public class ThirdPartyComponent    
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
