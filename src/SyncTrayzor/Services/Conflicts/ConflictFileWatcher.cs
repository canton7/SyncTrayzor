using NLog;
using Pri.LongPath;
using SyncTrayzor.SyncThing;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.Services.Conflicts
{
    public interface IConflictFileWatcher : IDisposable
    {
        bool IsEnabled { get; set; }
        List<string> ConflictedFiles { get; }
        TimeSpan FolderExistenceCheckingInterval { get; set; }

        event EventHandler ConflictedFilesChanged;
    }

    public class ConflictFileWatcher : IConflictFileWatcher
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const string versionsFolder = ".stversions";

        private readonly ISyncThingManager syncThingManager;
        private readonly IConflictFileManager conflictFileManager;

        private readonly object conflictedFilesLock = new object();
        // Contains all of the unique conflicted files, resolved from conflictFileOptions
        private List<string> conflictedFiles = new List<string>();

        private readonly object conflictFileOptionsLock = new object();
        // Contains all of the .sync-conflict files found
        private readonly HashSet<string> conflictFileOptions = new HashSet<string>();

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

            this.syncThingManager.Folders.FoldersChanged += this.FoldersChanged;
        }

        private void FoldersChanged(object sender, EventArgs e)
        {
            this.Reset();
        }

        private async void Reset()
        {
            this.StopWatchers();

            if (this.IsEnabled)
            {
                var folders = this.syncThingManager.Folders.FetchAll();

                this.StartWatchers(folders);
                await this.ScanFoldersAsync(folders);
            }
            else
            {
                lock (this.conflictFileOptionsLock)
                {
                    this.conflictFileOptions.Clear();
                }
                this.RefreshConflictedFiles();
            }
        }
        
        private void RefreshConflictedFiles()
        {
            var conflictFiles = new HashSet<string>();

            lock (this.conflictFileOptionsLock)
            {
                foreach (var conflictedFile in this.conflictFileOptions)
                {
                    ParsedConflictFileInfo parsedConflictFileInfo;
                    if (this.conflictFileManager.TryFindBaseFileForConflictFile(conflictedFile, out parsedConflictFileInfo))
                    {
                        conflictFiles.Add(parsedConflictFileInfo.OriginalPath);
                    }
                }
            }

            lock (this.conflictedFilesLock)
            {
                this.conflictedFiles = conflictFiles.ToList();
            }

            this.ConflictedFilesChanged?.Invoke(this, EventArgs.Empty);
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
                logger.Debug("Starting watcher for folder: {0}", folder.FolderId);

                var watcher = new FileWatcher(FileWatcherMode.CreatedOrDeleted, folder.Path, this.FolderExistenceCheckingInterval, this.conflictFileManager.ConflictPattern);
                watcher.FileChanged += this.FileChanged;
                this.fileWatchers.Add(watcher);
            }
        }

        private void FileChanged(object sender, FileChangedEventArgs e)
        {
            if (e.Path.StartsWith(versionsFolder))
                return;

            var fullPath = Path.Combine(e.Directory, e.Path);

            logger.Debug("Conflict file changed: {0} FileExists: {0}", fullPath, e.FileExists);

            bool changed;

            lock (this.conflictFileOptionsLock)
            {
                if (e.FileExists)
                    changed = this.conflictFileOptions.Add(fullPath);
                else
                    changed = this.conflictFileOptions.Remove(fullPath);
            }

            if (changed)
                this.RefreshConflictedFiles();
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
                    HashSet<string> oldConflictFileOptions;
                    lock (this.conflictFileOptionsLock)
                    {
                        oldConflictFileOptions = new HashSet<string>(this.conflictFileOptions);
                        this.conflictFileOptions.Clear();
                    }

                    foreach (var folder in folders)
                    {
                        logger.Debug("Scanning folder {0} for conflict files", folder.FolderId);

                        await this.conflictFileManager.FindConflicts(folder.Path, this.scanCts.Token).SubscribeAsync(conflict =>
                        {
                            lock (this.conflictFileOptionsLock)
                            {
                                foreach (var conflictOptions in conflict.Conflicts)
                                {
                                    this.conflictFileOptions.Add(Path.Combine(folder.Path, conflictOptions.FilePath));
                                }
                            }
                        });
                    }

                    lock (this.conflictFileOptionsLock)
                    {
                        conflictedFilesChanged = !this.conflictFileOptions.SetEquals(oldConflictFileOptions);
                    }

                }
                finally
                {
                    this.scanCts = null;
                }
            }

            if (conflictedFilesChanged)
                this.RefreshConflictedFiles();
        }

        public void Dispose()
        {
            this.StopWatchers();
            this.syncThingManager.Folders.FoldersChanged -= this.FoldersChanged;
        }
    }
}
