using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SyncTrayzor.Syncthing.Folders
{
    public class FolderIgnores
    {
        public IReadOnlyList<string> IgnorePatterns { get; }
        public IReadOnlyList<Regex> IncludeRegex { get; }
        public IReadOnlyList<Regex> ExcludeRegex { get; }

        public FolderIgnores()
        {
            this.IgnorePatterns = EmptyList<string>.Instance;
            this.IncludeRegex = EmptyList<Regex>.Instance;
            this.ExcludeRegex = EmptyList<Regex>.Instance;
        }

        public FolderIgnores(List<string> ignores, List<string> patterns)
        {
            this.IgnorePatterns = ignores;
            var includeRegex = new List<Regex>();
            var excludeRegex = new List<Regex>();

            foreach (var pattern in patterns)
            {
                if (pattern.StartsWith("(?exclude)"))
                    excludeRegex.Add(new Regex(pattern.Substring("(?exclude)".Length)));
                else
                    includeRegex.Add(new Regex(pattern));
            }

            this.IncludeRegex = includeRegex.AsReadOnly();
            this.ExcludeRegex = excludeRegex.AsReadOnly();
        }

        private static class EmptyList<T>
        {
            public static IReadOnlyList<T> Instance = new List<T>().AsReadOnly();
        }
    }
}
