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

        TimeSpan BackoffInterval { get; set; }
        TimeSpan FolderExistenceCheckingInterval { get; set; }

        event EventHandler ConflictedFilesChanged;
    }

    public class ConflictFileWatcher : IConflictFileWatcher
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const string versionsFolder = ".stversions";

        private readonly ISyncThingManager syncThingManager;
        private readonly IConflictFileManager conflictFileManager;
        private readonly IFileWatcherFactory fileWatcherFactory;

        private readonly object conflictedFilesLock = new object();
        // Contains all of the unique conflicted files, resolved from conflictFileOptions
        private List<string> conflictedFiles = new List<string>();

        private readonly object conflictFileOptionsLock = new object();
        // Contains all of the .sync-conflict files found
        private readonly HashSet<string> conflictFileOptions = new HashSet<string>();

        private readonly List<FileWatcher> fileWatchers = new List<FileWatcher>();

        private readonly SemaphoreSlim scanLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource scanCts;

        private readonly object backoffTimerLock = new object();
        private readonly System.Timers.Timer backoffTimer;

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

        public TimeSpan BackoffInterval { get; set; } =  TimeSpan.FromSeconds(10); // Need a default here

        public TimeSpan FolderExistenceCheckingInterval { get; set; }

        public event EventHandler ConflictedFilesChanged;

        public ConflictFileWatcher(
            ISyncThingManager syncThingManager,
            IConflictFileManager conflictFileManager,
            IFileWatcherFactory fileWatcherFactory)
        {
            this.syncThingManager = syncThingManager;
            this.conflictFileManager = conflictFileManager;
            this.fileWatcherFactory = fileWatcherFactory;

            this.syncThingManager.StateChanged += this.SyncThingStateChanged;
            this.syncThingManager.Folders.FoldersChanged += this.FoldersChanged;

            this.backoffTimer = new System.Timers.Timer() // Interval will be set when it's started
            {
                AutoReset = false,
            };
            this.backoffTimer.Elapsed += (o, e) =>
            {
                this.RefreshConflictedFiles();
            };
        }

        private void SyncThingStateChanged(object sender, SyncThingStateChangedEventArgs e)
        {
            this.Reset();
        }

        private void FoldersChanged(object sender, EventArgs e)
        {
            this.Reset();
        }

        private void RestartBackoffTimer()
        {
            lock (this.backoffTimerLock)
            {
                this.backoffTimer.Stop();
                this.backoffTimer.Interval = this.BackoffInterval.TotalMilliseconds;
                this.backoffTimer.Start();
            }
        }

        private async void Reset()
        {
            this.StopWatchers();

            if (this.IsEnabled && this.syncThingManager.State == SyncThingState.Running)
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
            logger.Info("Refreshing conflicted files");

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

                var watcher = this.fileWatcherFactory.Create(FileWatcherMode.CreatedOrDeleted, folder.Path, this.FolderExistenceCheckingInterval, this.conflictFileManager.ConflictPattern);
                watcher.FileChanged += this.FileChanged;
                this.fileWatchers.Add(watcher);
            }
        }

        private void FileChanged(object sender, FileChangedEventArgs e)
        {
            if (e.Path.StartsWith(versionsFolder) || Path.GetFileName(e.Path).StartsWith("~syncthing~"))
                return;

            var fullPath = Path.Combine(e.Directory, e.Path);

            logger.Debug("Conflict file changed: {0} FileExists: {1}", fullPath, e.FileExists);

            bool changed;

            lock (this.conflictFileOptionsLock)
            {
                if (e.FileExists)
                    changed = this.conflictFileOptions.Add(fullPath);
                else
                    changed = this.conflictFileOptions.Remove(fullPath);
            }

            if (changed)
                this.RestartBackoffTimer();
        }

        private async Task ScanFoldersAsync(IReadOnlyCollection<Folder> folders)
        {
            if (folders.Count == 0)
                return;

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

                    // If we get aborted, we won't refresh the conflicted files: it'll get done again in a minute anyway
                    bool conflictedFilesChanged;
                    lock (this.conflictFileOptionsLock)
                    {
                        conflictedFilesChanged = !this.conflictFileOptions.SetEquals(oldConflictFileOptions);
                    }

                    if (conflictedFilesChanged)
                        this.RestartBackoffTimer();

                }
                catch (OperationCanceledException) { }
                catch (AggregateException e) when (e.InnerException is OperationCanceledException) { }
                finally
                {
                    this.scanCts = null;
                }
            }
        }

        public void Dispose()
        {
            this.StopWatchers();
            this.syncThingManager.StateChanged -= this.SyncThingStateChanged;
            this.syncThingManager.Folders.FoldersChanged -= this.FoldersChanged;
            this.backoffTimer.Stop();
            this.backoffTimer.Dispose();
        }
    }
}
