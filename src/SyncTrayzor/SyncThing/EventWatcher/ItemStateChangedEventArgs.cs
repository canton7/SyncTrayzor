using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.EventWatcher
{
    public class ItemStateChangedEventArgs : EventArgs
    {
        public string Folder { get; private set; }
        public string Item { get; private set; }

        public ItemStateChangedEventArgs(string folder, string item)
        {
            this.Folder = folder;
            this.Item = item;
        }
    }
}
