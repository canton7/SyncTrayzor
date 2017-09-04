using NLog;
using Pri.LongPath;
using SyncTrayzor.Syncthing;
using SyncTrayzor.Syncthing.Folders;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
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

        private readonly ISyncthingManager syncthingManager;
        private readonly IConflictFileManager conflictFileManager;
        private readonly IFileWatcherFactory fileWatcherFactory;

        // Locks both conflictedFiles and conflictFileOptions
        private readonly object conflictFileRecordsLock = new object();

        // Contains all of the unique conflicted files, resolved from conflictFileOptions
        private List<string> conflictedFiles = new List<string>();

        // Contains all of the .sync-conflict files found
        private readonly HashSet<string> conflictFileOptions = new HashSet<string>();

        private readonly object fileWatchersLock = new object();
        private readonly List<FileWatcher> fileWatchers = new List<FileWatcher>();

        private readonly SemaphoreSlim scanLock = new SemaphoreSlim(1, 1);
        private CancellationTokenSource scanCts;

        private readonly object backoffTimerLock = new object();
        private readonly System.Timers.Timer backoffTimer;

        public List<string> ConflictedFiles
        {
            get
            {
                lock (this.conflictFileRecordsLock)
                {
                    return this.conflictedFiles.ToList();
                }
            }
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => this._isEnabled;
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
            ISyncthingManager syncthingManager,
            IConflictFileManager conflictFileManager,
            IFileWatcherFactory fileWatcherFactory)
        {
            this.syncthingManager = syncthingManager;
            this.conflictFileManager = conflictFileManager;
            this.fileWatcherFactory = fileWatcherFactory;

            this.syncthingManager.StateChanged += this.SyncthingStateChanged;
            this.syncthingManager.Folders.FoldersChanged += this.FoldersChanged;

            this.backoffTimer = new System.Timers.Timer() // Interval will be set when it's started
            {
                AutoReset = false,
            };
            this.backoffTimer.Elapsed += (o, e) =>
            {
                this.RefreshConflictedFiles();
            };
        }

        private void SyncthingStateChanged(object sender, SyncthingStateChangedEventArgs e)
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

            if (this.IsEnabled && this.syncthingManager.State == SyncthingState.Running)
            {
                var folders = this.syncthingManager.Folders.FetchAll();

                this.StartWatchers(folders);
                await this.ScanFoldersAsync(folders);
            }
            else
            {
                lock (this.conflictFileRecordsLock)
                {
                    this.conflictFileOptions.Clear();
                }
                this.RefreshConflictedFiles();
            }
        }
        
        private void RefreshConflictedFiles()
        {
            var conflictFiles = new HashSet<string>();

            lock (this.conflictFileRecordsLock)
            {
                foreach (var conflictedFile in this.conflictFileOptions)
                {
                    if (this.conflictFileManager.TryFindBaseFileForConflictFile(conflictedFile, out var parsedConflictFileInfo))
                    {
                        conflictFiles.Add(parsedConflictFileInfo.OriginalPath);
                    }
                }

                this.conflictedFiles = conflictFiles.ToList();

                logger.Debug($"Refreshing conflicted files. Found {this.conflictedFiles.Count} from {this.conflictFileOptions.Count} options");
            }

            this.ConflictedFilesChanged?.Invoke(this, EventArgs.Empty);
        }

        private void StopWatchers()
        {
            lock (this.fileWatchersLock)
            {
                foreach (var watcher in this.fileWatchers)
                {
                    watcher.Dispose();
                }

                this.fileWatchers.Clear();
            }
        }

        private void StartWatchers(IReadOnlyCollection<Folder> folders)
        {
            lock (this.fileWatchersLock)
            {
                foreach (var folder in folders)
                {
                    logger.Debug("Starting watcher for folder: {0} ({1})", folder.FolderId, folder.Label);

                    var watcher = this.fileWatcherFactory.Create(FileWatcherMode.CreatedOrDeleted, folder.Path, this.FolderExistenceCheckingInterval, this.conflictFileManager.ConflictPattern);
                    watcher.PathChanged += this.PathChanged;
                    this.fileWatchers.Add(watcher);
                }
            }
        }

        private void PathChanged(object sender, PathChangedEventArgs e)
        {
            var fullPath = Path.Combine(e.Directory, e.Path);

            if (this.conflictFileManager.IsPathIgnored(fullPath) || this.conflictFileManager.IsFileIgnored(fullPath))
                return;

            logger.Debug("Conflict file changed: {0} FileExists: {1}", fullPath, e.PathExists);

            bool changed;

            lock (this.conflictFileRecordsLock)
            {
                if (e.PathExists)
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
                    var newConflictFileOptions = new HashSet<string>();

                    foreach (var folder in folders)
                    {
                        logger.Debug("Scanning folder {0} ({1}) ({2}) for conflict files", folder.FolderId, folder.Label, folder.Path);

                        var options = await this.conflictFileManager.FindConflicts(folder.Path)
                            .SelectMany(conflict => conflict.Conflicts)
                            .Select(conflictOptions => Path.Combine(folder.Path, conflictOptions.FilePath))
                            .ToList()
                            .ToTask(this.scanCts.Token);

                        newConflictFileOptions.UnionWith(options);
                    }

                    // If we get aborted, we won't refresh the conflicted files: it'll get done again in a minute anyway
                    bool conflictedFilesChanged;
                    lock (this.conflictFileRecordsLock)
                    {
                        conflictedFilesChanged = !this.conflictFileOptions.SetEquals(newConflictFileOptions);
                        if (conflictedFilesChanged)
                        {
                            this.conflictFileOptions.Clear();
                            foreach (var file in newConflictFileOptions)
                            {
                                this.conflictFileOptions.Add(file);
                            }
                        }
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
            this.syncthingManager.StateChanged -= this.SyncthingStateChanged;
            this.syncthingManager.Folders.FoldersChanged -= this.FoldersChanged;
            this.backoffTimer.Stop();
            this.backoffTimer.Dispose();
        }
    }
}
