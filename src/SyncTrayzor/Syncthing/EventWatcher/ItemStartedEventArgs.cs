using SyncTrayzor.Syncthing.ApiClient;

namespace SyncTrayzor.Syncthing.EventWatcher
{
    public class ItemStartedEventArgs : ItemStateChangedEventArgs
    {
        public ItemChangedActionType Action { get; }
        public ItemChangedItemType ItemType { get; }

        public ItemStartedEventArgs(string folder, string item, ItemChangedActionType action, ItemChangedItemType itemType)
            : base(folder, item)
        {
            this.Action = action;
            this.ItemType = itemType;
        }
    }
}
