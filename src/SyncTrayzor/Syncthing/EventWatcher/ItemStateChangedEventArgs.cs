using System;

namespace SyncTrayzor.Syncthing.EventWatcher
{
    public class ItemStateChangedEventArgs : EventArgs
    {
        public string Folder { get; }
        public string Item { get; }

        public ItemStateChangedEventArgs(string folder, string item)
        {
            this.Folder = folder;
            this.Item = item;
        }
    }
}
