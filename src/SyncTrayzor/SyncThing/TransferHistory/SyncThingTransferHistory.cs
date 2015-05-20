using SyncTrayzor.SyncThing.EventWatcher;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.TransferHistory
{
    public interface ISyncThingTransferHistory
    {
        event EventHandler<FileTransferChangedEventArgs> TransferStateChanged;
        event EventHandler<FileTransferChangedEventArgs> TransferStarted;
        event EventHandler<FileTransferChangedEventArgs> TransferCompleted;

        IEnumerable<FileTransfer> CompletedTransfers { get; }
        IEnumerable<FileTransfer> InProgressTransfers { get; }
    }

    public class SyncThingTransferHistory : ISyncThingTransferHistory
    {
        private readonly ISyncThingEventWatcher eventWatcher;
        private readonly SynchronizedEventDispatcher eventDispatcher;

        private const int maxCompletedTransfers = 100;

        // Locks both completedTransfers and inProgressTransfers
        private readonly object transfersLock = new object();

        // It's a queue because we limit its length
        private readonly Queue<FileTransfer> completedTransfers = new Queue<FileTransfer>();

        // Key is the string 'FolderId:Path'
        private readonly Dictionary<string, FileTransfer> inProgressTransfers = new Dictionary<string, FileTransfer>();

        public event EventHandler<FileTransferChangedEventArgs> TransferStateChanged;
        public event EventHandler<FileTransferChangedEventArgs> TransferStarted;
        public event EventHandler<FileTransferChangedEventArgs> TransferCompleted;

        public IEnumerable<FileTransfer> CompletedTransfers
        {
            get
            {
                lock (this.transfersLock)
                {
                    return this.completedTransfers.ToArray();
                }
            }
        }

        public IEnumerable<FileTransfer> InProgressTransfers
        {
            get
            {
                lock (this.transfersLock)
                {
                    return this.inProgressTransfers.Values.ToArray();
                }
            }
        }

        public SyncThingTransferHistory(ISyncThingEventWatcher eventWatcher)
        {
            this.eventDispatcher = new SynchronizedEventDispatcher(this);

            this.eventWatcher = eventWatcher;

            this.eventWatcher.ItemStarted += this.ItemStarted;
            this.eventWatcher.ItemFinished += this.ItemFinished;
            this.eventWatcher.ItemDownloadProgressChanged += this.ItemDownloadProgressChanged;
        }

        private FileTransfer FetchOrInsertInProgressFileTransfer(string folder, string path)
        {
            var key = this.KeyForFileTransfer(folder, path);
            bool created = false;
            FileTransfer fileTransfer;
            lock (this.transfersLock)
            {
                if (!this.inProgressTransfers.TryGetValue(key, out fileTransfer))
                {
                    created = true;
                    fileTransfer = new FileTransfer(folder, path);
                    this.inProgressTransfers.Add(key, fileTransfer);
                }
            }

            if (created)
                this.OnTransferStarted(fileTransfer);
            return fileTransfer;
        }

        private void ItemStarted(object sender, ItemStateChangedEventArgs e)
        {
            this.FetchOrInsertInProgressFileTransfer(e.Folder, e.Item);
        }

        private void ItemFinished(object sender, ItemStateChangedEventArgs e)
        {
            // It *should* be in the 'in progress transfers'...
            FileTransfer fileTransfer;
            lock (this.transfersLock)
            {
                var key = this.KeyForFileTransfer(e.Folder, e.Item);
                fileTransfer = this.FetchOrInsertInProgressFileTransfer(e.Folder, e.Item);
                fileTransfer.SetComplete();
                this.inProgressTransfers.Remove(key);

                this.completedTransfers.Enqueue(fileTransfer);
                if (this.completedTransfers.Count > maxCompletedTransfers)
                    this.completedTransfers.Dequeue();
            }

            if (fileTransfer != null)
            {
                this.OnTransferStateChanged(fileTransfer);
                this.OnTransferCompleted(fileTransfer);
            }
        }

        private void ItemDownloadProgressChanged(object sender, ItemDownloadProgressChangedEventArgs e)
        {
            var fileTransfer = this.FetchOrInsertInProgressFileTransfer(e.Folder, e.Item);
            fileTransfer.SetDownloadProgress(e.BytesDone, e.BytesTotal);

            this.OnTransferStateChanged(fileTransfer);
        }

        private string KeyForFileTransfer(string folderId, string path)
        {
            return String.Format("{0}:{1}", folderId, path);
        }

        private void OnTransferStateChanged(FileTransfer fileTransfer)
        {
            this.eventDispatcher.Raise(this.TransferStateChanged, new FileTransferChangedEventArgs(fileTransfer));
        }

        private void OnTransferStarted(FileTransfer fileTransfer)
        {
            this.eventDispatcher.Raise(this.TransferStarted, new FileTransferChangedEventArgs(fileTransfer));
        }

        private void OnTransferCompleted(FileTransfer fileTransfer)
        {
            this.eventDispatcher.Raise(this.TransferCompleted, new FileTransferChangedEventArgs(fileTransfer));
        }
    }
}
