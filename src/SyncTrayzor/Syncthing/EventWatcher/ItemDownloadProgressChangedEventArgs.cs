namespace SyncTrayzor.Syncthing.EventWatcher
{
    public class ItemDownloadProgressChangedEventArgs : ItemStateChangedEventArgs
    {
        public long BytesDone { get; }
        public long BytesTotal { get; }

        public ItemDownloadProgressChangedEventArgs(string folder, string item, long bytesDone, long bytesTotal)
            : base(folder, item)
        {
            this.BytesDone = bytesDone;
            this.BytesTotal = bytesTotal;
        }
    }
}
