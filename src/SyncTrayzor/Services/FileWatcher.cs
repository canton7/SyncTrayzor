using NLog;
using SyncTrayzor.Utils;
using System;
using System.IO;
using System.Linq;
using System.Timers;
using Path = Pri.LongPath.Path;

namespace SyncTrayzor.Services
{
    [Flags]
    public enum FileWatcherMode
    {
        ContentChanged = 1,
        CreatedOrDeleted = 2,
        All = ContentChanged | CreatedOrDeleted
    }

    public class PathChangedEventArgs : EventArgs
    {
        public string Directory { get; }
        public string Path { get; }
        public bool PathExists { get; }

        public PathChangedEventArgs(string directory, string path, bool pathExists)
        {
            this.Directory = directory;
            this.Path = path;
            this.PathExists = pathExists;
        }
    }

    public interface IFileWatcherFactory
    {
        FileWatcher Create(FileWatcherMode mode, string directory, TimeSpan existenceCheckingInterval, string filter = "*.*");
    }

    public class FileWatcherFactory : IFileWatcherFactory
    {
        private readonly IFilesystemProvider filesystem;

        public FileWatcherFactory(IFilesystemProvider filesystem)
        {
            this.filesystem = filesystem;
        }

        public FileWatcher Create(FileWatcherMode mode, string directory, TimeSpan existenceCheckingInterval, string filter = "*.*")
        {
            return new FileWatcher(this.filesystem, mode, directory, existenceCheckingInterval, filter);
        }
    }

    public class FileWatcher : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IFilesystemProvider filesystem;
        private readonly FileWatcherMode mode;
        private readonly string filter;
        protected readonly string Directory;
        private readonly Timer existenceCheckingTimer;

        private FileSystemWatcher watcher;

        public event EventHandler<PathChangedEventArgs> PathChanged;

        public FileWatcher(IFilesystemProvider filesystem, FileWatcherMode mode, string directory, TimeSpan existenceCheckingInterval, string filter = "*.*")
        {
            this.filesystem = filesystem;
            this.mode = mode;
            this.filter = filter;
            this.Directory = directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            this.watcher = this.TryToCreateWatcher(this.Directory);

            this.existenceCheckingTimer = new Timer()
            {
                AutoReset = true,
                Interval = existenceCheckingInterval.TotalMilliseconds,
                Enabled = true,
            };
            this.existenceCheckingTimer.Elapsed += (o, e) => this.CheckExistence();
        }

        private FileSystemWatcher TryToCreateWatcher(string directory)
        {
            try
            {
                var watcher = new FileSystemWatcher()
                {
                    Path = directory,
                    Filter = this.filter,
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                };

                if (this.mode.HasFlag(FileWatcherMode.ContentChanged))
                {
                    watcher.Changed += this.OnChanged;
                }
                if (this.mode.HasFlag(FileWatcherMode.CreatedOrDeleted))
                {
                    watcher.Created += this.OnCreated;
                    watcher.Deleted += this.OnDeleted;
                    watcher.Renamed += this.OnRenamed;
                }

                watcher.EnableRaisingEvents = true;

                return watcher;
            }
            catch (ArgumentException)
            {
                logger.Warn("Watcher for {0} couldn't be created: path doesn't exist", this.Directory);
                // The path doesn't exist. That's fine, the existenceCheckingTimer will try and
                // re-create us shortly if needs be
                return null;
            }
            catch (FileNotFoundException e)
            {
                // This can happen if e.g. the user points us towards 'My Documents' on Vista+, and we get an
                // 'Error reading the xxx directory'
                logger.Warn($"Watcher for {this.Directory} couldn't be created: {e.Message}", e);
                // We'll try again soon
                return null;
            }
        }

        private void CheckExistence()
        {
            var exists = System.IO.Directory.Exists(this.Directory);
            if (exists && this.watcher == null)
            {
                logger.Debug("Path {0} appeared. Creating watcher", this.Directory);
                this.watcher = this.TryToCreateWatcher(this.Directory);
            }
            else if (!exists && this.watcher != null)
            {
                logger.Debug("Path {0} disappeared. Destroying watcher", this.Directory);
                this.watcher.Dispose();
                this.watcher = null;
            }
        }

        private void OnDeleted(object source, FileSystemEventArgs e)
        {
            if (e.FullPath == null)
            {
                logger.Warn("OnDeleted: e.FullPath is null. Ignoring...");
                return;
            }

            this.RecordPathChange(e.FullPath, pathExists: false);
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            if (e.FullPath == null)
            {
                logger.Warn("OnChnaged: e.FullPath is null. Ignoring...");
                return;
            }

            // We don't want to raise Changed events on directories - those are file creations or deletions.
            // Creations will pop up in OnCreated, deletions in OnDeletion.
            // We do however want to handle file changes

            try
            {
                if (this.filesystem.FileExists(e.FullPath))
                    this.RecordPathChange(e.FullPath, pathExists: true);
            }
            catch (Exception ex)
            {
                logger.Warn(ex, $"Failed to see whether file/dir {e.FullPath} exists");
            }
        }

        private void OnCreated(object source, FileSystemEventArgs e)
        {
            if (e.FullPath == null)
            {
                logger.Warn("OnCreated: e.FullPath is null. Ignoring...");
                return;
            }

            this.RecordPathChange(e.FullPath, pathExists: true);
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            // Apparently e.FullPath or e.OldName can be null, see #112
            // Not sure why this might be, but let's work around it...
            if (e.FullPath == null)
            {
                logger.Warn("OnRenamed: e.FullPath is null. Ignoring...");
                return;
            }

            this.RecordPathChange(e.FullPath, pathExists: true);
            // Irritatingly, e.OldFullPath will throw an exception if the path is longer than the windows max
            // (but e.FullPath is fine).
            // So, construct it from e.FullPath and e.OldName.
            // Note that we're using Pri.LongPath to get a Path.GetDirectoryName implementation that can handle
            // long paths

            if (e.OldName == null)
            {
                logger.Warn("OnRenamed: e.OldName is null. Not sure why");
                return;
            }

            // Note that e.FullPath could be a file or a directory. If it's a directory, it could be a drive
            // root. If it's a drive root, Path.GetDirectoryName will return null. I'm not sure *how* it could
            // be a drive root though... Record a change to the drive root if so.
            var oldFullPathDirectory = Path.GetDirectoryName(e.FullPath);
            if (oldFullPathDirectory == null)
            {
                this.RecordPathChange(e.FullPath, pathExists: true);
            }
            else
            {
                var oldFileName = Path.GetFileName(e.OldName);
                var oldFullPath = Path.Combine(oldFullPathDirectory, oldFileName);

                this.RecordPathChange(oldFullPath, pathExists: false);
            }
        }

        private void RecordPathChange(string path, bool pathExists)
        {
            // First, we need to convert to a long path, just in case anyone's using the short path
            // We can't do this if we don't expect the file to exist any more...
            // There's also a chance that the file no longer exists. Catch that exception.
            // If a short path is renamed or deleted, then we do our best with it in a bit, by removing the short bits
            // If short path segments are used in the base directory path in this case, tough.
            if (pathExists && path.Contains("~"))
                path = this.GetLongPathName(path);

            // https://msdn.microsoft.com/en-us/library/dd465121.aspx
            if (!path.StartsWith(this.Directory, StringComparison.OrdinalIgnoreCase))
            {
                logger.Warn($"Ignoring change to {path}, as it isn't in {this.Directory}");
                return;
            }

            var subPath = path.Substring(this.Directory.Length);

            // If it contains a tilde, then it's a short path that squeezed through GetLongPath above
            // (e.g. because it was a deletion), then strip it back to the first component without an ~
            subPath = this.StripShortPathSegments(subPath);

            this.OnPathChanged(subPath, pathExists);
        }

        public virtual void OnPathChanged(string path, bool pathExists)
        {
            this.PathChanged?.Invoke(this, new PathChangedEventArgs(this.Directory, path, pathExists));
        }

        private string GetLongPathName(string path)
        {
            try
            {
                path = PathEx.GetLongPathName(path);
            }
            catch (FileNotFoundException)
            {
                logger.Warn($"Path {path} changed, but it doesn't exist any more");
            }

            return path;
        }

        private string StripShortPathSegments(string path)
        {
            if (!path.Contains('~'))
                return path;

            var parts = path.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            var filteredPaths = parts.TakeWhile(x => !x.Contains('~'));
            return String.Join(Path.DirectorySeparatorChar.ToString(), filteredPaths);
        }

        public virtual void Dispose()
        {
            if (this.watcher != null)
            {
                this.watcher.EnableRaisingEvents = false;
                this.watcher.Dispose();
            }

            this.existenceCheckingTimer.Stop();
            this.existenceCheckingTimer.Dispose();
        }
    }
}
