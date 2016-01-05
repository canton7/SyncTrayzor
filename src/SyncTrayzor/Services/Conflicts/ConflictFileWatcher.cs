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
        bool IsEnabled { get; set; }
        List<string> ConflictedFiles { get; }
        TimeSpan FolderExistenceCheckingInterval { get; set; }

        event EventHandler ConflictedFilesChanged;
    }

    public class ConflictFileWatcher : IConflictFileWatcher, IDisposable
    {
        private const string versionsFolder = ".stversions";
        private const string conflictFileMarker = ".sync-conflict-";
        private const string conflictFilePattern = "*.sync-conflict-*";

        private readonly ISyncThingManager syncThingManager;
        private readonly IConflictFileManager conflictFileManager;

        private readonly object conflictedFilesLock = new object();
        private readonly HashSet<string> conflictedFiles = new HashSet<string>();

        private readonly List<FileWatcher> fileWatchers = new List<FileWatcher>();

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

        private bool _isEnabled;
        public bool IsEnabled
        {
            get { return this._isEnabled; }
            set
            {
                if (this._isEnabled == value)
                    return;

                this._isEnabled = value;
                this.Reset();
            }
        }

        public TimeSpan FolderExistenceCheckingInterval { get; set; }

        public event EventHandler ConflictedFilesChanged;

        public ConflictFileWatcher(
            ISyncThingManager syncThingManager,
            IConflictFileManager conflictFileManager)
        {
            this.syncThingManager = syncThingManager;
            this.conflictFileManager = conflictFileManager;

            this.syncThingManager.Folders.FoldersChanged += (o, e) => this.Reset();
        }

        private async void Reset()
        {
            this.StopWatchers();

            if (!this.IsEnabled)
                return;

            var folders = this.syncThingManager.Folders.FetchAll();

            this.StartWatchers(folders);
            await this.ScanFoldersAsync(folders);
        }

        private void StopWatchers()
        {
            foreach (var watcher in this.fileWatchers)
            {
                watcher.Dispose();
            }

            this.fileWatchers.Clear();
        }

        private void StartWatchers(IReadOnlyCollection<Folder> folders)
        {
            foreach (var folder in folders)
            {
                var watcher = new FileWatcher(FileWatcherMode.CreatedOrDeleted, folder.Path, this.FolderExistenceCheckingInterval, conflictFilePattern);
                watcher.FileChanged += this.FileChanged;
                this.fileWatchers.Add(watcher);
            }
        }

        private void FileChanged(object sender, FileChangedEventArgs e)
        {
            if (e.Path.StartsWith(versionsFolder))
                return;

            var fullPath = Path.Combine(e.Directory, e.Path);

            // TODO: This needs more work
            // We don't handle it being deleted properly. We need ot see whether there are *any* conflict
            // files for this file. Maybe keep a collection of file -> conflicts? We'll have to handle failure
            // to find the base path properly...

            // Can we find an original file for it?
            ParsedConflictFileInfo parsedConflictFileInfo;
            if (!this.conflictFileManager.TryFindBaseFileForConflictFile(fullPath, out parsedConflictFileInfo))
                return;

            bool changed;

            lock (this.conflictedFilesLock)
            {
                if (e.FileExists)
                    changed = this.conflictedFiles.Add(parsedConflictFileInfo.OriginalPath);
                else
                    changed = this.conflictedFiles.Remove(parsedConflictFileInfo.OriginalPath);
            }

            if (changed)
                this.ConflictedFilesChanged?.Invoke(this, EventArgs.Empty);
        }

        private async Task ScanFoldersAsync(IReadOnlyCollection<Folder> folders)
        {
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
                                this.conflictedFiles.Add(Path.Combine(folder.Path, conflict.File.FilePath));
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

        public void Dispose()
        {
            this.StopWatchers();
        }
    }
}
