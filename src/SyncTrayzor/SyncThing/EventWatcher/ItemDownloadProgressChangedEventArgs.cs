using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.EventWatcher
{
    public class ItemDownloadProgressChangedEventArgs : ItemStateChangedEventArgs
    {
        public long BytesDone { get; private set; }
        public long BytesTotal { get; private set; }

        public ItemDownloadProgressChangedEventArgs(string folder, string item, long bytesDone, long bytesTotal)
            : base(folder, item)
        {
            this.BytesDone = bytesDone;
            this.BytesTotal = bytesTotal;
        }
    }
}
