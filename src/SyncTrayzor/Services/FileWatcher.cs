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

    public class FileChangedEventArgs : EventArgs
    {
        public string Directory { get; }
        public string Path { get; }
        public bool FileExists { get; }

        public FileChangedEventArgs(string directory, string path, bool fileExists)
        {
            this.Directory = directory;
            this.Path = path;
            this.FileExists = fileExists;
        }
    }

    public class FileWatcher : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly FileWatcherMode mode;
        private readonly string filter;
        protected readonly string Directory;
        private readonly Timer existenceCheckingTimer;

        private FileSystemWatcher watcher;

        public event EventHandler<FileChangedEventArgs> FileChanged;

        public FileWatcher(FileWatcherMode mode, string directory, TimeSpan existenceCheckingInterval, string filter = "*.*")
        {
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
                    watcher.Changed += this.OnChangedOrCreated;
                }
                if (this.mode.HasFlag(FileWatcherMode.CreatedOrDeleted))
                {
                    watcher.Created += this.OnChangedOrCreated;
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
                logger.Info("Path {0} appeared. Creating watcher", this.Directory);
                this.watcher = this.TryToCreateWatcher(this.Directory);
            }
            else if (!exists && this.watcher != null)
            {
                logger.Info("Path {0} disappeared. Destroying watcher", this.Directory);
                this.watcher.Dispose();
                this.watcher = null;
            }
        }

        private void OnDeleted(object source, FileSystemEventArgs e)
        {
            this.PathChanged(e.FullPath, fileExists: false);
        }

        private void OnChangedOrCreated(object source, FileSystemEventArgs e)
        {
            this.PathChanged(e.FullPath, fileExists: true);
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            this.PathChanged(e.FullPath, fileExists: true);
            // Irritatingly, e.OldFullPath will throw an exception if the path is longer than the windows max
            // (but e.FullPath is fine).
            // So, construct it from e.FullPath and e.OldName
            // Note that we're using Pri.LongPath to get a Path.GetDirectoryName implementation that can handle
            // long paths

            // Apparently Path.GetDirectoryName(e.FullPath) or Path.GetFileName(e.OldName) can return null, see #112
            // Not sure why this might be, but let's work around it...
            var oldFullPathDirectory = Path.GetDirectoryName(e.FullPath);
            var oldFileName = Path.GetFileName(e.OldName);

            if (oldFullPathDirectory == null)
            {
                logger.Warn("OldFullPathDirectory is null. Not sure why... e.FullPath: {0}", e.FullPath);
                return;
            }

            if (oldFileName == null)
            {
                logger.Warn("OldFileName is null. Not sure why... e.OldName: {0}", e.OldName);
                return;
            }

            var oldFullPath = Path.Combine(oldFullPathDirectory, oldFileName);

            this.PathChanged(oldFullPath, fileExists: false);
        }

        private void PathChanged(string path, bool fileExists)
        {
            // First, we need to convert to a long path, just in case anyone's using the short path
            // We can't do this if we don't expect the file to exist any more...
            // There's also a chance that the file no longer exists. Catch that exception.
            // If a short path is renamed or deleted, then we do our best with it in a bit, by removing the short bits
            // If short path segments are used in the base directory path in this case, tough.
            if (fileExists)
                path = this.GetLongPathName(path);

            if (!path.StartsWith(this.Directory))
                return;

            var subPath = path.Substring(this.Directory.Length);

            // If it contains a tilde, then it's a short path that squeezed through GetLongPath above
            // (e.g. because it was a deletion), then strip it back to the first component without an ~
            subPath = this.StripShortPathSegments(subPath);

            this.OnFileChanged(subPath, fileExists);
        }

        public virtual void OnFileChanged(string path, bool fileExists)
        {
            var handler = this.FileChanged;
            if (handler != null)
                handler(this, new FileChangedEventArgs(this.Directory, path, fileExists));
        }

        private string GetLongPathName(string path)
        {
            try
            {
                path = PathEx.GetLongPathName(path);
            }
            catch (FileNotFoundException e)
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
