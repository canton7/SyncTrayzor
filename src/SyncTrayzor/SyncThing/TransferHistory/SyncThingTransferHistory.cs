using NLog;
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
        event EventHandler<FolderSynchronizationFinishedEventArgs> FolderSynchronizationFinished;

        IEnumerable<FileTransfer> CompletedTransfers { get; }
        IEnumerable<FileTransfer> InProgressTransfers { get; }
    }

    public class SyncThingTransferHistory : ISyncThingTransferHistory
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly ISyncThingEventWatcher eventWatcher;
        private readonly SynchronizedEventDispatcher eventDispatcher;

        private const int maxCompletedTransfers = 100;

        // Locks both completedTransfers, inProgressTransfers, and recentlySynchronized
        private readonly object transfersLock = new object();

        // It's a queue because we limit its length
        private readonly Queue<FileTransfer> completedTransfers = new Queue<FileTransfer>();

        // Key is the string 'FolderId:Path'
        private readonly Dictionary<string, FileTransfer> inProgressTransfers = new Dictionary<string, FileTransfer>();

        // Collection of stuff synchronized recently. Keyed on folder. Cleared when that folder finished synchronizing
        private readonly Dictionary<string, List<FileTransfer>> recentlySynchronized = new Dictionary<string, List<FileTransfer>>();

        public event EventHandler<FileTransferChangedEventArgs> TransferStateChanged;
        public event EventHandler<FileTransferChangedEventArgs> TransferStarted;
        public event EventHandler<FileTransferChangedEventArgs> TransferCompleted;
        public event EventHandler<FolderSynchronizationFinishedEventArgs> FolderSynchronizationFinished;

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
            this.eventWatcher.SyncStateChanged += this.SyncStateChanged;
        }

        private FileTransfer FetchOrInsertInProgressFileTransfer(string folder, string path, ItemChangedItemType itemType, ItemChangedActionType actionType)
        {
            var key = this.KeyForFileTransfer(folder, path);
            bool created = false;
            FileTransfer fileTransfer;
            lock (this.transfersLock)
            {
                if (!this.inProgressTransfers.TryGetValue(key, out fileTransfer))
                {
                    created = true;
                    fileTransfer = new FileTransfer(folder, path, itemType, actionType);
                    logger.Debug("Created file transfer: {0}", fileTransfer);
                    this.inProgressTransfers.Add(key, fileTransfer);
                }
            }

            if (created)
                this.OnTransferStarted(fileTransfer);
            return fileTransfer;
        }

        private void ItemStarted(object sender, ItemStartedEventArgs e)
        {
            logger.Debug("Item started. Folder: {0}, Item: {1}, Type: {2}, Action: {3}", e.Folder, e.Item, e.ItemType, e.Action);
            this.FetchOrInsertInProgressFileTransfer(e.Folder, e.Item, e.ItemType, e.Action);
        }

        private void ItemFinished(object sender, ItemFinishedEventArgs e)
        {
            logger.Debug("Item finished. Folder: {0}, Item: {1}, Type: {2}, Action: {3}", e.Folder, e.Item, e.ItemType, e.Action);
            // It *should* be in the 'in progress transfers'...
            FileTransfer fileTransfer;
            lock (this.transfersLock)
            {
                var key = this.KeyForFileTransfer(e.Folder, e.Item);
                fileTransfer = this.FetchOrInsertInProgressFileTransfer(e.Folder, e.Item, e.ItemType, e.Action);
                fileTransfer.SetComplete(e.Error);
                this.inProgressTransfers.Remove(key);

                logger.Debug("File Transfer set to complete: {0}", fileTransfer);

                this.completedTransfers.Enqueue(fileTransfer);
                if (this.completedTransfers.Count > maxCompletedTransfers)
                    this.completedTransfers.Dequeue();

                List<FileTransfer> recentlySynchronizedList;
                if (!this.recentlySynchronized.TryGetValue(e.Folder, out recentlySynchronizedList))
                {
                    recentlySynchronizedList = new List<FileTransfer>();
                    this.recentlySynchronized[e.Folder] = recentlySynchronizedList;
                }
                recentlySynchronizedList.Add(fileTransfer);
            }

            this.OnTransferStateChanged(fileTransfer);
            this.OnTransferCompleted(fileTransfer);
        }

        private void ItemDownloadProgressChanged(object sender, ItemDownloadProgressChangedEventArgs e)
        {
            logger.Debug("Item progress changed. Folder: {0}, Item: {1}", e.Folder, e.Item);
            // If we didn't see the started event, tough. We don't have enough information to re-create it...
            var key = this.KeyForFileTransfer(e.Folder, e.Item);
            FileTransfer fileTransfer;
            lock (this.transfersLock)
            {
                if (!this.inProgressTransfers.TryGetValue(key, out fileTransfer))
                    return; // Nothing we can do...

                fileTransfer.SetDownloadProgress(e.BytesDone, e.BytesTotal);
                logger.Debug("File transfer progress changed: {0}", fileTransfer);
            }

            this.OnTransferStateChanged(fileTransfer);
        }

        private void SyncStateChanged(object sender, SyncStateChangedEventArgs e)
        {
            if (e.PrevSyncState == FolderSyncState.Syncing)
            {
                List<FileTransfer> transferredList = null;

                lock (this.transfersLock)
                {
                    if (this.recentlySynchronized.TryGetValue(e.FolderId, out transferredList))
                        this.recentlySynchronized.Remove(e.FolderId);
                }

                this.OnFolderSynchronizationFinished(e.FolderId, transferredList ?? new List<FileTransfer>());
            }
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

        private void OnFolderSynchronizationFinished(string folderId, List<FileTransfer> fileTransfers)
        {
            this.eventDispatcher.Raise(this.FolderSynchronizationFinished, new FolderSynchronizationFinishedEventArgs(folderId, fileTransfers));
        }
    }
}
