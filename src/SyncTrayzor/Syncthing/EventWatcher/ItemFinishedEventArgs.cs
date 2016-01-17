using SyncTrayzor.Syncthing.ApiClient;

namespace SyncTrayzor.Syncthing.EventWatcher
{
    public class ItemFinishedEventArgs : ItemStateChangedEventArgs
    {
        public ItemChangedActionType Action { get; }
        public ItemChangedItemType ItemType { get; }
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
