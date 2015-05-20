using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.EventWatcher
{
    public enum ItemChangedActionType
    {
        Update,
        Delete,
        Unknown,
    }

    public class ItemStartedEventArgs : ItemStateChangedEventArgs
    {
        public ItemChangedActionType Action { get; private set; }

        public ItemStartedEventArgs(string folder, string item, ItemChangedActionType action)
            : base(folder, item)
        {
            this.Action = action;
        }
    }
}
