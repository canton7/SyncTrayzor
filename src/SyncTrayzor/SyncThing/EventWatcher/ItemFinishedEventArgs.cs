using SyncTrayzor.SyncThing.ApiClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.EventWatcher
{
    public class ItemFinishedEventArgs : ItemStateChangedEventArgs
    {
        public ItemChangedActionType Action { get; private set; }
        public ItemChangedItemType ItemType { get; private set; }
        public string Error { get; private set; }

        public ItemFinishedEventArgs(string folder, string item, ItemChangedActionType action, ItemChangedItemType itemType, string error)
            : base(folder, item)
        {
            this.Action = action;
            this.ItemType = itemType;
            this.Error = error;
        }
    }
}
