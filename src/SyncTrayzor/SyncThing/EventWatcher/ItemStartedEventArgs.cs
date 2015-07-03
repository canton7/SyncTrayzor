using SyncTrayzor.SyncThing.ApiClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.EventWatcher
{
    public class ItemStartedEventArgs : ItemStateChangedEventArgs
    {
        public ItemChangedActionType Action { get; private set; }
        public ItemChangedItemType ItemType { get; private set; }

        public ItemStartedEventArgs(string folder, string item, ItemChangedActionType action, ItemChangedItemType itemType)
            : base(folder, item)
        {
            this.Action = action;
            this.ItemType = itemType;
        }
    }
}
