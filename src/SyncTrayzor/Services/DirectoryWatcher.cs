using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

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
        private readonly FileSystemWatcher watcher;

        private readonly object currentNotifySubPathLock = new object();
        private string currentNotifyingSubPath;
        private readonly Timer backoffTimer;

        public event EventHandler<PreviewDirectoryChangedEventArgs> PreviewDirectoryChanged;
        public event EventHandler<DirectoryChangedEventArgs> DirectoryChanged;

        public DirectoryWatcher(string directory, TimeSpan backoffInterval)
        {
            if (backoffInterval.Ticks < 0)
                throw new ArgumentException("backoffInterval must be > 0");

            this.directory = directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            this.watcher = this.CreateWatcher(this.directory);

            this.backoffTimer = new Timer()
            {
                AutoReset = false,
                Interval = backoffInterval.TotalMilliseconds,
            };
            this.backoffTimer.Elapsed += (o, e) =>
            {
                string currentNotifyingSubPath;
                lock (this.currentNotifySubPathLock)
                {
                    currentNotifyingSubPath = this.currentNotifyingSubPath;
                    this.currentNotifyingSubPath = null;
                }
                this.OnDirectoryChanged(currentNotifyingSubPath);
            };
        }

        private FileSystemWatcher CreateWatcher(string directory)
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

        private void OnChanged(object source, FileSystemEventArgs e)
        {
            this.PathChanged(e.FullPath);
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            this.PathChanged(e.FullPath);
            this.PathChanged(e.OldFullPath);
        }

        private void PathChanged(string path)
        {
            if (!path.StartsWith(this.directory))
                return;

            var subPath = path.Substring(this.directory.Length);
            if (this.OnPreviewDirectoryChanged(subPath))
                return;

            this.backoffTimer.Stop();
            lock (this.currentNotifySubPathLock)
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

        // Return true to cancel
        private bool OnPreviewDirectoryChanged(string subPath)
        {
            var handler = this.PreviewDirectoryChanged;
            if (handler != null)
            {
                var ea = new PreviewDirectoryChangedEventArgs(this.directory, subPath);
                handler(this, ea);
                logger.Debug("PreviewDirectoryChanged with path {0}. Cancelled: {1}", Path.Combine(this.directory, subPath), ea.Cancel);
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
            this.watcher.EnableRaisingEvents = false;
            this.backoffTimer.Stop();

            this.watcher.Dispose();
            this.backoffTimer.Dispose();
        }
    }
}
