using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public enum FolderSyncState
    {
        Syncing,
        Idle,
    }

    public class FolderIgnores
    {
        public List<string> IgnorePatterns { get; private set; }
        public List<Regex> IncludeRegex { get; private set; }
        public List<Regex> ExcludeRegex { get; private set; }

        public FolderIgnores(List<string> ignores, List<string> patterns)
        {
            this.IgnorePatterns = ignores;
            this.IncludeRegex = new List<Regex>();
            this.ExcludeRegex = new List<Regex>();

            foreach (var pattern in patterns)
            {
                if (pattern.StartsWith("(?exclude)"))
                    this.ExcludeRegex.Add(new Regex(pattern.Substring("(?exclude)".Length)));
                else
                    this.IncludeRegex.Add(new Regex(pattern));
            }
        }
    }

    public class Folder
    {
        public string FolderId { get; private set; }
        public string Path { get; private set; }
        public FolderSyncState SyncState { get; set; }
        public HashSet<string> SyncthingPaths { get; private set; }
        public FolderIgnores Ignores { get; set; }

        public Folder(string folderId, string path, FolderIgnores ignores)
        {
            this.FolderId = folderId;
            this.Path = path;
            this.SyncState = FolderSyncState.Idle;
            this.SyncthingPaths = new HashSet<string>();
            this.Ignores = ignores;
        }
    }
}
