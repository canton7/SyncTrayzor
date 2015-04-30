using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

using Path = Pri.LongPath.Path;

namespace SyncTrayzor.Services
{
    public class DirectoryChangedEventArgs : EventArgs
    {
        public string DirectoryPath { get; private set; }
        public string SubPath { get; private set; }

        public DirectoryChangedEventArgs(string directoryPath, string subPath)
        {
            this.DirectoryPath = directoryPath;
            this.SubPath = subPath;
        }
    }

    public class PreviewDirectoryChangedEventArgs : DirectoryChangedEventArgs
    {
        public bool Cancel { get; set; }

        public PreviewDirectoryChangedEventArgs(string directoryPath, string subPath)
            : base(directoryPath, subPath) { }
    }

    public class DirectoryWatcher : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly string directory;

        private readonly Timer backoffTimer;
        private readonly Timer existenceCheckingTimer;

        private readonly object currentNotifyingSubPathLock = new object();
        private string currentNotifyingSubPath;
        private FileSystemWatcher watcher;

        public event EventHandler<PreviewDirectoryChangedEventArgs> PreviewDirectoryChanged;
        public event EventHandler<DirectoryChangedEventArgs> DirectoryChanged;

        public DirectoryWatcher(string directory, TimeSpan backoffInterval, TimeSpan existenceCheckingInterval)
        {
            if (backoffInterval.Ticks < 0)
                throw new ArgumentException("backoffInterval must be > 0");

            this.directory = directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            this.watcher = this.TryToCreateWatcher(this.directory);

            this.backoffTimer = new Timer()
            {
                AutoReset = false,
                Interval = backoffInterval.TotalMilliseconds,
            };
            this.backoffTimer.Elapsed += (o, e) =>
            {
                string currentNotifyingSubPath;
                lock (this.currentNotifyingSubPathLock)
                {
                    currentNotifyingSubPath = this.currentNotifyingSubPath;
                    this.currentNotifyingSubPath = null;
                }
                this.OnDirectoryChanged(currentNotifyingSubPath);
            };

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
                    Filter = "*.*",
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                };

                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Deleted += OnChanged;
                watcher.Renamed += OnRenamed;

                watcher.EnableRaisingEvents = true;

                return watcher;
            }
            catch (ArgumentException)
            {
                logger.Warn("Watcher for {0} couldn't be created: path doesn't exist", this.directory);
                // The path doesn't exist. That's fine, the existenceCheckingTimer will try and
                // re-create us shortly if needs be
                return null;
            }
        }

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            this.PathChanged(e.FullPath);
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            this.PathChanged(e.FullPath);
            // Irritatingly, e.OldFullPath will throw an exception if the path is longer than the windows max
            // (but e.FullPath is fine).
            // So, construct it from e.FullPath and e.OldName
            // Note that we're using Pri.LongPath to get a Path.GetDirectoryName implementation that can handle
            // long paths
            var oldFullPath = Path.Combine(Path.GetDirectoryName(e.FullPath), Path.GetFileName(e.OldName));
            this.PathChanged(oldFullPath);
        }

        private void PathChanged(string path)
        {
            if (!path.StartsWith(this.directory))
                return;

            var subPath = path.Substring(this.directory.Length);
            if (this.OnPreviewDirectoryChanged(subPath))
                return;

            this.backoffTimer.Stop();
            lock (this.currentNotifyingSubPathLock)
            {
                if (this.currentNotifyingSubPath == null)
                    this.currentNotifyingSubPath = subPath;
                else
                    this.currentNotifyingSubPath = this.FindCommonPrefix(this.currentNotifyingSubPath, subPath);
            }

            this.backoffTimer.Start();
        }

        private string FindCommonPrefix(string path1, string path2)
        {
            var parts1 = path1.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
            var parts2 = path2.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            var result = new List<string>();
            for (int i = 0; i < Math.Min(parts1.Length, parts2.Length); i++)
            {
                if (parts1[i] != parts2[i])
                    break;

                result.Add(parts1[i]);
            }

            return String.Join(Path.DirectorySeparatorChar.ToString(), result);
        }

        private void CheckExistence()
        {
            var exists = Directory.Exists(this.directory);
            if (exists && this.watcher == null)
            {
                logger.Info("Path {0} appeared. Creating watcher", this.directory);
                this.watcher = this.TryToCreateWatcher(this.directory);
            }
            else if (!exists && this.watcher != null)
            {
                logger.Info("Path {0} disappeared. Destroying watcher", this.directory);
                this.watcher.Dispose();
                this.watcher = null;
            }
        }

        // Return true to cancel
        private bool OnPreviewDirectoryChanged(string subPath)
        {
            var handler = this.PreviewDirectoryChanged;
            if (handler != null)
            {
                var ea = new PreviewDirectoryChangedEventArgs(this.directory, subPath);
                handler(this, ea);
                logger.Trace("PreviewDirectoryChanged with path {0}. Cancelled: {1}", Path.Combine(this.directory, subPath), ea.Cancel);
                return ea.Cancel;
            }
            return false;
        }

        private void OnDirectoryChanged(string subPath)
        {
            logger.Info("Path Changed: {0}", Path.Combine(this.directory, subPath));
            var handler = this.DirectoryChanged;
            if (handler != null)
                handler(this, new DirectoryChangedEventArgs(this.directory, subPath));
        }

        public void Dispose()
        {
            if (this.watcher != null)
            {
                this.watcher.EnableRaisingEvents = false;
                this.watcher.Dispose();
            }

            this.backoffTimer.Stop();
            this.backoffTimer.Dispose();

            this.existenceCheckingTimer.Stop();
            this.existenceCheckingTimer.Dispose();
        }
    }
}
