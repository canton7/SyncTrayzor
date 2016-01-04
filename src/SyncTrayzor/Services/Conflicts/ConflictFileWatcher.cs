using Pri.LongPath;
using SyncTrayzor.SyncThing;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.Conflicts
{
    public interface IConflictFileWatcher
    {
        List<string> ConflictedFiles { get; }

        event EventHandler ConflictedFilesChanged;
    }

    public class ConflictFileWatcher : IConflictFileWatcher
    {
        private const string conflictFileMarker = ".sync-conflict-";

        private readonly ISyncThingManager syncThingManager;
        private readonly IWatchedFolderMonitor watchedFolderMonitor;
        private readonly IConflictFileManager conflictFileManager;

        private readonly object conflictedFilesLock = new object();
        private readonly HashSet<string> conflictedFiles = new HashSet<string>();

        private readonly SemaphoreSlim scanLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource scanCts;

        public List<string> ConflictedFiles
        {
            get
            {
                lock (this.conflictedFilesLock)
                {
                    return this.conflictedFiles.ToList();
                }
            }
        }

        public event EventHandler ConflictedFilesChanged;

        public ConflictFileWatcher(
            ISyncThingManager syncThingManager,
            IWatchedFolderMonitor watchedFolderMonitor,
            IConflictFileManager conflictFileManager)
        {
            this.syncThingManager = syncThingManager;
            this.watchedFolderMonitor = watchedFolderMonitor;
            this.conflictFileManager = conflictFileManager;

            this.syncThingManager.Folders.FoldersChanged += (o, e) => this.Reset();

            this.watchedFolderMonitor.FileChangeDetected += this.FileChangeDetected;
        }

        private void FileChangeDetected(object sender, FileChangeDetectedEventArgs e)
        {
            if (!Path.GetFileName(e.Path).Contains(conflictFileMarker))
                return;

            bool changed;

            lock (this.conflictedFilesLock)
            {
                if (e.FileExists)
                    changed = this.conflictedFiles.Add(e.Path);
                else
                    changed = this.conflictedFiles.Remove(e.Path);
            }

            if (changed)
                this.ConflictedFilesChanged?.Invoke(this, EventArgs.Empty);
        }

        private async void Reset()
        {
            var folders = this.syncThingManager.Folders.FetchAll();

            if (folders.Count == 0)
                return;

            bool conflictedFilesChanged;

            // We're not re-entrant. There's a CTS which will abort the previous invocation, but we'll need to wait
            // until that happens
            this.scanCts?.Cancel();
            using (await this.scanLock.WaitAsyncDisposable())
            {
                this.scanCts = new CancellationTokenSource();
                try
                {
                    HashSet<string> oldConflictedFiles;
                    lock (this.conflictedFilesLock)
                    {
                        oldConflictedFiles = new HashSet<string>(this.conflictedFiles);
                        this.conflictedFiles.Clear();
                    }

                    foreach (var folder in folders)
                    {
                        await this.conflictFileManager.FindConflicts(folder.Path, this.scanCts.Token).SubscribeAsync(conflict =>
                        {
                            lock (this.conflictedFilesLock)
                            {
                                foreach (var file in conflict.Conflicts)
                                {
                                    this.conflictedFiles.Add(file.FilePath);
                                }
                            }
                        });
                    }

                    lock (this.conflictedFilesLock)
                    {
                        conflictedFilesChanged = !this.conflictedFiles.SetEquals(oldConflictedFiles);
                    }

                }
                finally
                {
                    this.scanCts = null;
                }
            }

            if (conflictedFilesChanged)
                this.ConflictedFilesChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
