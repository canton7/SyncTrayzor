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

    public interface IConflictFileManager
    {
        IObservable<ConflictSet> FindConflicts(string basePath, CancellationToken cancellationToken);
        void ResolveConflict(ConflictSet conflictSet, string chosenFilePath);
    }

    public class ConflictFileManager : IConflictFileManager
    {
        private const string conflictPattern = "*.sync-conflict-*";
        private static readonly Regex conflictRegex =
            new Regex(@"^(?<prefix>.*).sync-conflict-(?<year>\d{4})(?<month>\d{2})(?<day>\d{2})-(?<hours>\d{2})(?<mins>\d{2})(?<secs>\d{2})(?<suffix>.*)(?<extension>\..*)$");
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IFilesystemProvider filesystemProvider;

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

            var conflictLookup = new Dictionary<string, List<ParsedConflictFileInfo>>();
            var stack = new Stack<string>();
            stack.Push(basePath);
            while (stack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                conflictLookup.Clear();
                var directory = stack.Pop();

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

                foreach (var kvp in conflictLookup)
                {
                    var file = new ConflictFile(kvp.Key, this.filesystemProvider.GetLastWriteTime(kvp.Key));
                    var conflicts = kvp.Value.Select(x => new ConflictOption(x.FilePath, this.filesystemProvider.GetLastWriteTime(x.FilePath), x.Created)).ToList();
                    subject.Next(new ConflictSet(file, conflicts));
                }

                foreach (var subDirectory in this.filesystemProvider.EnumerateDirectories(directory, "*", System.IO.SearchOption.TopDirectoryOnly))
                {
                    if (subDirectory == ".stversions")
                        continue;

                    stack.Push(Path.Combine(directory, subDirectory));

                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        private bool TryFindBaseFileForConflictFile(string filePath, out ParsedConflictFileInfo parsedConflictFileInfo)
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

            var dateCreated = new DateTime(year, month, day, hours, mins, secs, DateTimeKind.Local);

            // 'suffix' might be a versioner thing (~date-time), or it might be something added by another tool...
            // Try searching for it, and if that fails go without

            var withSuffix = prefix + suffix + extension;
            if (this.filesystemProvider.FileExists(Path.Combine(directory, withSuffix)))
            {
                parsedConflictFileInfo = new ParsedConflictFileInfo(filePath, Path.Combine(directory, withSuffix), dateCreated);
                return true;
            }

            var withoutSuffix = prefix + extension;
            if (this.filesystemProvider.FileExists(Path.Combine(directory, withoutSuffix)))
            {
                parsedConflictFileInfo = new ParsedConflictFileInfo(filePath, Path.Combine(directory, withoutSuffix), dateCreated);
                return true;
            }

            parsedConflictFileInfo = default(ParsedConflictFileInfo);
            return false;
        }

        public void ResolveConflict(ConflictSet conflictSet, string chosenFilePath)
        {
            if (chosenFilePath != conflictSet.File.FilePath && !conflictSet.Conflicts.Any(x => x.FilePath == chosenFilePath))
                throw new ArgumentException("chosenPath does not exist inside conflictSet");

            if (chosenFilePath == conflictSet.File.FilePath)
            {
                foreach (var file in conflictSet.Conflicts)
                {
                    logger.Debug("Deleting {0}", file);
                    this.filesystemProvider.DeleteFileToRecycleBin(file.FilePath);
                }
            }
            else
            {
                logger.Debug("Deleting {0}", conflictSet.File.FilePath);
                this.filesystemProvider.DeleteFileToRecycleBin(conflictSet.File.FilePath);

                foreach (var file in conflictSet.Conflicts)
                {
                    if (file.FilePath == chosenFilePath)
                        continue;

                    logger.Debug("Deleting {0}", file.FilePath);
                    this.filesystemProvider.DeleteFileToRecycleBin(file.FilePath);
                }

                logger.Debug("Renaming {0} to {1}", chosenFilePath, conflictSet.File.FilePath);
                this.filesystemProvider.MoveFile(chosenFilePath, conflictSet.File.FilePath);
            }
        }

        private struct ParsedConflictFileInfo
        {
            public readonly string FilePath;
            public readonly string OriginalPath;
            public readonly DateTime Created;

            public ParsedConflictFileInfo(string filePath, string originalPath, DateTime created)
            {
                this.FilePath = filePath;
                this.OriginalPath = originalPath;
                this.Created = created;
            }
        }
    }
}
