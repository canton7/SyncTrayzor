using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Pri.LongPath;
using NLog;
using SyncTrayzor.Utils;

namespace SyncTrayzor.Services.Conflicts
{
    public class ConflictFile
    {
        public string FilePath { get; }
        public DateTime LastModified { get; }

        public ConflictFile(string filePath, DateTime lastModified)
        {
            this.FilePath = filePath;
            this.LastModified = lastModified;
        }

        public override string ToString()
        {
            return this.FilePath;
        }
    }

    public class ConflictOption
    {
        public string FilePath { get; }
        public DateTime LastModified { get; }

        public DateTime Created { get; }

        public ConflictOption(string filePath, DateTime lastModified, DateTime created)
        {
            this.FilePath = filePath;
            this.LastModified = lastModified;
            this.Created = created;
        }

        public override string ToString()
        {
            return this.FilePath;
        }
    }

    public class ConflictSet
    {
        public ConflictFile File { get; }
        public List<ConflictOption> Conflicts { get; }

        public ConflictSet(ConflictFile file, List<ConflictOption> conflicts)
        {
            this.File = file;
            this.Conflicts = conflicts;
        }
    }

    public struct ParsedConflictFileInfo
    {
        public string FilePath { get; }
        public string OriginalPath { get; }
        public DateTime Created { get; }

        public ParsedConflictFileInfo(string filePath, string originalPath, DateTime created)
        {
            this.FilePath = filePath;
            this.OriginalPath = originalPath;
            this.Created = created;
        }
    }

    public interface IConflictFileManager
    {
        string ConflictPattern { get; }

        IObservable<ConflictSet> FindConflicts(string basePath, CancellationToken cancellationToken);
        void ResolveConflict(ConflictSet conflictSet, string chosenFilePath, bool deleteToRecycleBin);
        bool TryFindBaseFileForConflictFile(string filePath, out ParsedConflictFileInfo parsedConflictFileInfo);
    }

    public class ConflictFileManager : IConflictFileManager
    {
        private const string conflictPattern = "*.sync-conflict-*";
        private const string stVersionsFolder = ".stversions";
        private static readonly Regex conflictRegex =
            new Regex(@"^(?<prefix>.*).sync-conflict-(?<year>\d{4})(?<month>\d{2})(?<day>\d{2})-(?<hours>\d{2})(?<mins>\d{2})(?<secs>\d{2})(?<suffix>.*)(?<extension>\..*)$");
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private const int maxSearchDepth = 255; // Loosely based on the max path length (a bit over)

        private readonly IFilesystemProvider filesystemProvider;

        public string ConflictPattern => conflictPattern;

        public ConflictFileManager(IFilesystemProvider filesystemProvider)
        {
            this.filesystemProvider = filesystemProvider;
        }

        public IObservable<ConflictSet> FindConflicts(string basePath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var subject = new SlimObservable<ConflictSet>();
            Task.Run(() =>
            {
                try
                {
                    this.FindConflictsImpl(basePath, subject, cancellationToken);
                    subject.Complete();
                }
                catch (Exception e)
                {
                    subject.Error(e);
                }
            });
            return subject;
        }

        private void FindConflictsImpl(string basePath, SlimObservable<ConflictSet> subject, CancellationToken cancellationToken)
        {
            // We may find may conflict files for each conflict, and we need to group them.
            // We can't relay on the order returns by EnumerateFiles either, so it's hard to tell when we've spotted
            // all conflict files. Therefore we need to do this directory by directory, and flush out the cache
            // or conflicts after each directory.

            logger.Debug("Looking for conflicts in {0}", basePath);

            var conflictLookup = new Dictionary<string, List<ParsedConflictFileInfo>>();
            var stack = new Stack<SearchDirectory>();
            stack.Push(new SearchDirectory(basePath, 0));
            while (stack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                conflictLookup.Clear();
                var searchDirectory = stack.Pop();
                var directory = searchDirectory.Directory;

                this.TryFilesystemOperation(() =>
                {
                    foreach (var fileName in this.filesystemProvider.EnumerateFiles(directory, conflictPattern, System.IO.SearchOption.TopDirectoryOnly))
                    {
                        var filePath = Path.Combine(directory, fileName);

                        ParsedConflictFileInfo conflictFileInfo;
                        // We may not be able to parse it properly (conflictPattern is pretty basic), or it might not exist, or...
                        if (!this.TryFindBaseFileForConflictFile(filePath, out conflictFileInfo))
                            continue;

                        List<ParsedConflictFileInfo> existingConflicts;
                        if (!conflictLookup.TryGetValue(conflictFileInfo.OriginalPath, out existingConflicts))
                        {
                            existingConflicts = new List<ParsedConflictFileInfo>();
                            conflictLookup.Add(conflictFileInfo.OriginalPath, existingConflicts);
                        }
                        existingConflicts.Add(conflictFileInfo);

                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }, directory, "directories");

                foreach (var kvp in conflictLookup)
                {
                    var file = new ConflictFile(kvp.Key, this.filesystemProvider.GetLastWriteTime(kvp.Key));
                    var conflicts = kvp.Value.Select(x => new ConflictOption(x.FilePath, this.filesystemProvider.GetLastWriteTime(x.FilePath), x.Created)).ToList();
                    subject.Next(new ConflictSet(file, conflicts));
                }

                if (searchDirectory.Depth < maxSearchDepth)
                {
                    this.TryFilesystemOperation(() =>
                    {
                        foreach (var subDirectory in this.filesystemProvider.EnumerateDirectories(directory, "*", System.IO.SearchOption.TopDirectoryOnly))
                        {
                            if (subDirectory == stVersionsFolder)
                                continue;

                            stack.Push(new SearchDirectory(Path.Combine(directory, subDirectory), searchDirectory.Depth + 1));

                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }, directory, "files");
                }
                else
                {
                    logger.Warn($"Max search depth of {maxSearchDepth} exceeded with path {directory}. Not proceeding further.");
                }
            }
        }

        private void TryFilesystemOperation(Action action, string path, string itemType)
        {
            try
            {
                action();
            }
            catch (UnauthorizedAccessException)
            {
                // Expected with reparse points, etc
                logger.Warn($"UnauthorizedAccessException when trying to enumerate {itemType} in folder {path}");
            }
            catch (Exception e)
            {
                logger.Error($"Failed to enumerate {itemType} in folder {path}: {e.GetType().Name} {e.Message}", e);
            }
        }

        public bool TryFindBaseFileForConflictFile(string filePath, out ParsedConflictFileInfo parsedConflictFileInfo)
        {
            var directory = Path.GetDirectoryName(filePath);
            var fileName = Path.GetFileName(filePath);

            var parsed = conflictRegex.Match(fileName);
            if (!parsed.Success)
            {
                parsedConflictFileInfo = default(ParsedConflictFileInfo);
                return false;
            }

            var prefix = parsed.Groups["prefix"].Value;
            var year = Int32.Parse(parsed.Groups["year"].Value);
            var month = Int32.Parse(parsed.Groups["month"].Value);
            var day = Int32.Parse(parsed.Groups["day"].Value);
            var hours = Int32.Parse(parsed.Groups["hours"].Value);
            var mins = Int32.Parse(parsed.Groups["mins"].Value);
            var secs = Int32.Parse(parsed.Groups["secs"].Value);
            var suffix = parsed.Groups["suffix"].Value;
            var extension = parsed.Groups["extension"].Value;

            DateTime dateCreated;
            try
            {
                dateCreated = new DateTime(year, month, day, hours, mins, secs, DateTimeKind.Local);
            }
            catch (ArgumentException e)
            {
                // 31st Feb, etc
                logger.Error($"Failed to parse DateTime for file path {filePath}", e);
                parsedConflictFileInfo = default(ParsedConflictFileInfo);
                return false;
            }

            // 'suffix' might be a versioner thing (~date-time), or it might be something added by another tool...
            // Try searching for it, and if that fails go without

            try
            {
                var withSuffix = Path.Combine(directory, prefix + suffix + extension);
                if (this.filesystemProvider.FileExists(withSuffix))
                {
                    parsedConflictFileInfo = new ParsedConflictFileInfo(filePath, withSuffix, dateCreated);
                    return true;
                }

                var withoutSuffix = Path.Combine(directory, prefix + extension);
                if (this.filesystemProvider.FileExists(withoutSuffix))
                {
                    parsedConflictFileInfo = new ParsedConflictFileInfo(filePath, withoutSuffix, dateCreated);
                    return true;
                }
            }
            catch (Exception e)
            {
                // We're in the path to return false at this point
                logger.Error($"Failed to look for base file for conflict file {filePath}: {e.Message}", e);
            }

            parsedConflictFileInfo = default(ParsedConflictFileInfo);
            return false;
        }

        public void ResolveConflict(ConflictSet conflictSet, string chosenFilePath, bool deleteToRecycleBin)
        {
            if (chosenFilePath != conflictSet.File.FilePath && !conflictSet.Conflicts.Any(x => x.FilePath == chosenFilePath))
                throw new ArgumentException("chosenPath does not exist inside conflictSet");

            if (chosenFilePath == conflictSet.File.FilePath)
            {
                foreach (var file in conflictSet.Conflicts)
                {
                    logger.Debug("Deleting {0}", file);
                    this.DeleteFile(file.FilePath, deleteToRecycleBin);
                }
            }
            else
            {
                logger.Debug("Deleting {0}", conflictSet.File.FilePath);
                this.DeleteFile(conflictSet.File.FilePath, deleteToRecycleBin);

                foreach (var file in conflictSet.Conflicts)
                {
                    if (file.FilePath == chosenFilePath)
                        continue;

                    logger.Debug("Deleting {0}", file.FilePath);
                    this.DeleteFile(file.FilePath, deleteToRecycleBin);
                }

                logger.Debug("Renaming {0} to {1}", chosenFilePath, conflictSet.File.FilePath);
                this.filesystemProvider.MoveFile(chosenFilePath, conflictSet.File.FilePath);
            }
        }

        private void DeleteFile(string path, bool deleteToRecycleBin)
        {
            if (deleteToRecycleBin)
                this.filesystemProvider.DeleteFileToRecycleBin(path);
            else
                this.filesystemProvider.DeleteFile(path);
        }

        private struct SearchDirectory
        {
            public readonly string Directory;
            public readonly int Depth;

            public SearchDirectory(string directory, int depth)
            {
                this.Directory = directory;
                this.Depth = depth;
            }
        }
    }
}
