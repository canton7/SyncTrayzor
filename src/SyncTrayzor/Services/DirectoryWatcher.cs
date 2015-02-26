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

    public class DirectoryWatcher : IDisposable
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly string directory;
        private readonly FileSystemWatcher watcher;

        private readonly object currentNotifySubPathLock = new object();
        private string currentNotifyingSubPath;
        private readonly Timer backoffTimer;


        public event EventHandler<DirectoryChangedEventArgs> DirectoryChanged;

        public DirectoryWatcher(string directory)
        {
            this.directory = directory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            this.watcher = this.CreateWatcher(this.directory);

            this.backoffTimer = new Timer()
            {
                AutoReset = false,
                Interval = 1000.0, // ms
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

            this.backoffTimer.Stop();
            var subPath = path.Substring(this.directory.Length);

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

        private void OnDirectoryChanged(string subPath)
        {
            logger.Debug("Path Changed: {0}", Path.Combine(this.directory, subPath));
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
