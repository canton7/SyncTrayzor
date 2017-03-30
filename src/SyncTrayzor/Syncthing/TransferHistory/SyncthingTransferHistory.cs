using NLog;
using SyncTrayzor.Syncthing.ApiClient;
using SyncTrayzor.Syncthing.EventWatcher;
using SyncTrayzor.Syncthing.Folders;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SyncTrayzor.Syncthing.TransferHistory
{
    public interface ISyncthingTransferHistory : IDisposable
    {
        event EventHandler<FileTransferChangedEventArgs> TransferStateChanged;
        event EventHandler<FileTransferChangedEventArgs> TransferStarted;
        event EventHandler<FileTransferChangedEventArgs> TransferCompleted;
        event EventHandler<FolderSynchronizationFinishedEventArgs> FolderSynchronizationFinished;

        IEnumerable<FileTransfer> CompletedTransfers { get; }
        IEnumerable<FileTransfer> InProgressTransfers { get; }
        IEnumerable<FailingTransfer> FailingTransfers { get; }
    }

    public class SyncthingTransferHistory : ISyncthingTransferHistory
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly Logger downloadLogger = LogManager.GetLogger("DownloadLog");

        private readonly ISyncthingEventWatcher eventWatcher;
        private readonly ISyncthingFolderManager folderManager;
        private readonly SynchronizedEventDispatcher eventDispatcher;

        private const int maxCompletedTransfers = 100;

        // Locks both completedTransfers, inProgressTransfers, and recentlySynchronized
        private readonly object transfersLock = new object();

        // It's a queue because we limit its length
        private readonly Queue<FileTransfer> completedTransfers = new Queue<FileTransfer>();

        private readonly Dictionary<FolderPathKey, FileTransfer> inProgressTransfers = new Dictionary<FolderPathKey, FileTransfer>();
        private readonly Dictionary<FolderPathKey, FailingTransfer> currentlyFailingTransfers = new Dictionary<FolderPathKey, FailingTransfer>();

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

        public IEnumerable<FailingTransfer> FailingTransfers
        {
            get
            {
                lock (this.transfersLock)
                {
                    return this.currentlyFailingTransfers.Values.ToArray();
                }
            }
        }

        public SyncthingTransferHistory(ISyncthingEventWatcher eventWatcher, ISyncthingFolderManager folderManager)
        {
            this.eventDispatcher = new SynchronizedEventDispatcher(this);

            this.eventWatcher = eventWatcher;
            this.folderManager = folderManager;

            this.eventWatcher.ItemStarted += this.ItemStarted;
            this.eventWatcher.ItemFinished += this.ItemFinished;
            this.eventWatcher.ItemDownloadProgressChanged += this.ItemDownloadProgressChanged;

            // We can't use the EventWatcher to watch for folder sync state change events: events could be skipped.
            // The folder manager knows how to listen to skipped event notifications, and refresh the folder states appropriately
            this.folderManager.SyncStateChanged += this.SyncStateChanged;
        }

        private FileTransfer FetchOrInsertInProgressFileTransfer(string folder, string path, ItemChangedItemType itemType, ItemChangedActionType actionType)
        {
            var key = new FolderPathKey(folder, path);
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
            // We only care about files or folders - no metadata please!
            if ((e.ItemType != ItemChangedItemType.File && e.ItemType != ItemChangedItemType.Dir) ||
                (e.Action != ItemChangedActionType.Update && e.Action != ItemChangedActionType.Delete))
            {
                return;
            }

            this.FetchOrInsertInProgressFileTransfer(e.Folder, e.Item, e.ItemType, e.Action);
        }

        private void ItemFinished(object sender, ItemFinishedEventArgs e)
        {
            // Folder,Path,Type,Action,Error
            downloadLogger.Info($"{e.Folder},{e.Item},{e.ItemType},{e.Action},{e.Error}");
            logger.Debug("Item finished. Folder: {0}, Item: {1}, Type: {2}, Action: {3}", e.Folder, e.Item, e.ItemType, e.Action);

            if ((e.ItemType != ItemChangedItemType.File && e.ItemType != ItemChangedItemType.Dir) ||
                (e.Action != ItemChangedActionType.Update && e.Action != ItemChangedActionType.Delete))
            {
                return;
            }

            // It *should* be in the 'in progress transfers'...
            FileTransfer fileTransfer;
            lock (this.transfersLock)
            {
                fileTransfer = this.FetchOrInsertInProgressFileTransfer(e.Folder, e.Item, e.ItemType, e.Action);

                this.CompleteFileTransfer(fileTransfer, e.Error);
            }

            this.OnTransferStateChanged(fileTransfer);
            this.OnTransferCompleted(fileTransfer);
        }

        private void CompleteFileTransfer(FileTransfer fileTransfer, string error)
        {
            // This is always called from within a lock, but you can't be too sure...
            lock (this.transfersLock)
            {
                var key = new FolderPathKey(fileTransfer.FolderId, fileTransfer.Path);

                bool isNewError = false;
                if (error == null)
                {
                    this.currentlyFailingTransfers.Remove(key);
                }
                else
                {
                    if (!this.currentlyFailingTransfers.TryGetValue(key, out var failingTransfer) || failingTransfer.Error != error)
                    {
                        // Remove will only do something in the case that the failure existed, but the error changed
                        this.currentlyFailingTransfers.Remove(key);
                        this.currentlyFailingTransfers.Add(key, new FailingTransfer(fileTransfer.FolderId, fileTransfer.Path, error));
                        isNewError = true;
                    }
                }

                fileTransfer.SetComplete(error, isNewError);
                this.inProgressTransfers.Remove(key);

                logger.Debug("File Transfer set to complete: {0}", fileTransfer);

                this.completedTransfers.Enqueue(fileTransfer);
                if (this.completedTransfers.Count > maxCompletedTransfers)
                    this.completedTransfers.Dequeue();

                if (!this.recentlySynchronized.TryGetValue(fileTransfer.FolderId, out var recentlySynchronizedList))
                {
                    recentlySynchronizedList = new List<FileTransfer>();
                    this.recentlySynchronized[fileTransfer.FolderId] = recentlySynchronizedList;
                }
                recentlySynchronizedList.Add(fileTransfer);
            }
        }

        private void ItemDownloadProgressChanged(object sender, ItemDownloadProgressChangedEventArgs e)
        {
            logger.Debug("Item progress changed. Folder: {0}, Item: {1}", e.Folder, e.Item);

            // If we didn't see the started event, tough. We don't have enough information to re-create it...
            var key = new FolderPathKey(e.Folder, e.Item);
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

        private void SyncStateChanged(object sender, FolderSyncStateChangedEventArgs e)
        {
            var folderId = e.FolderId;

            if (e.PrevSyncState == FolderSyncState.Syncing)
            {
                List<FileTransfer> transferredList = null;
                List<FileTransfer> completedFileTransfers; // Those that Syncthing didn't tell us had completed

                lock (this.transfersLock)
                {
                    // Syncthing may not have told us that a file has completed, because it can forget events.
                    // Therefore mark everything in this folder as having completed
                    completedFileTransfers = this.inProgressTransfers.Where(x => x.Key.Folder == folderId).Select(x => x.Value).ToList();
                    foreach (var completedFileTransfer in completedFileTransfers)
                    {
                        this.CompleteFileTransfer(completedFileTransfer, error: null);
                    }

                    if (this.recentlySynchronized.TryGetValue(folderId, out transferredList))
                        this.recentlySynchronized.Remove(folderId);
                }

                foreach (var fileTransfer in completedFileTransfers)
                {
                    this.OnTransferStateChanged(fileTransfer);
                    this.OnTransferCompleted(fileTransfer);
                }
                this.OnFolderSynchronizationFinished(folderId, transferredList ?? new List<FileTransfer>());
            }
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
            if (!this.folderManager.TryFetchById(folderId, out var folder))
                return;

            this.eventDispatcher.Raise(this.FolderSynchronizationFinished, new FolderSynchronizationFinishedEventArgs(folder, fileTransfers));
        }

        public void Dispose()
        {
            this.eventWatcher.ItemStarted -= this.ItemStarted;
            this.eventWatcher.ItemFinished -= this.ItemFinished;
            this.eventWatcher.ItemDownloadProgressChanged -= this.ItemDownloadProgressChanged;
            this.folderManager.SyncStateChanged -= this.SyncStateChanged;
        }

        private struct FolderPathKey : IEquatable<FolderPathKey>
        {
            public readonly string Folder;
            public readonly string Path;

            public FolderPathKey(string folder, string path)
            {
                this.Folder = folder;
                this.Path = path;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + this.Folder.GetHashCode();
                    hash = hash * 31 + this.Path.GetHashCode();
                    return hash;
                }
            }

            public override bool Equals(object obj)
            {
                return (obj is FolderPathKey) && this.Equals((FolderPathKey)obj);
            }

            public bool Equals(FolderPathKey other)
            {
                return this.Folder == other.Folder && this.Path == other.Path;
            }
        }
    }
}
