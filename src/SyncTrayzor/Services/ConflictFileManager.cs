using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using SyncTrayzor.Utils;

namespace SyncTrayzor.Services
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
        void ResolveConflict(ConflictSet conflictSet, ConflictFile chosenPath);
    }

    public class ConflictFileManager : IConflictFileManager
    {
        private const string conflictPattern = "*.sync-conflict-*";
        private static readonly Regex conflictRegex = new Regex(@"^(.*).sync-conflict-(\d{8}-\d{6})(.*)?(\..*)$");
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly IFilesystemProvider filesystemProvider;

        public ConflictFileManager(IFilesystemProvider filesystemProvider)
        {
            this.filesystemProvider = filesystemProvider;
        }

        public IObservable<ConflictSet> FindConflicts(string basePath, CancellationToken cancellationToken)
        {
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

            var conflictLookup = new Dictionary<string, List<string>>();
            var stack = new Stack<string>();
            stack.Push(basePath);
            while (stack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                conflictLookup.Clear();
                var directory = stack.Pop();

                foreach (var fileName in this.filesystemProvider.EnumerateFiles(directory, conflictPattern, SearchOption.TopDirectoryOnly))
                {
                    var file = Path.Combine(directory, fileName);
                    var original = this.FindBaseFileForConflictFile(directory, fileName);
                    // We may not be able to parse it properly (conflictPattern is pretty basic), or it might not exist, or...
                    if (original == null)
                        continue;

                    List<string> existingConflicts;
                    if (!conflictLookup.TryGetValue(original, out existingConflicts))
                    {
                        existingConflicts = new List<string>();
                        conflictLookup.Add(original, existingConflicts);
                    }
                    existingConflicts.Add(fileName);

                    cancellationToken.ThrowIfCancellationRequested();
                }

                foreach (var kvp in conflictLookup)
                {
                    var file = new ConflictFile(kvp.Key, this.filesystemProvider.GetLastWriteTime(kvp.Key));
                    // TODO: Compute the 'conflict created' time from the file name
                    var conflicts = kvp.Value.Select(x => new ConflictOption(x, this.filesystemProvider.GetLastWriteTime(x), DateTime.Now)).ToList();
                    subject.Next(new ConflictSet(file, conflicts));
                }

                foreach (var subDirectory in this.filesystemProvider.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly))
                {
                    if (subDirectory == ".stversions")
                        continue;

                    stack.Push(Path.Combine(directory, subDirectory));

                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        private string FindBaseFileForConflictFile(string directory, string conflictFileName)
        {
            var parsed = conflictRegex.Match(conflictFileName);
            if (!parsed.Success)
                return null;

            var prefix = parsed.Groups[1].Value;
            var suffix = parsed.Groups[3].Value;
            var extension = parsed.Groups[4].Value;

            // 'suffix' might be a versioner thing (~date-time), or it might be something added by another tool...
            // Try searching for it, and if that fails go without

            var withSuffix = prefix + suffix + extension;
            if (this.filesystemProvider.FileExists(Path.Combine(directory, withSuffix)))
                return withSuffix;

            var withoutSuffix = prefix + extension;
            if (this.filesystemProvider.FileExists(Path.Combine(directory, withoutSuffix)))
                return withoutSuffix;

            return null;
        }

        public void ResolveConflict(ConflictSet conflictSet, ConflictFile chosenFile)
        {
            if (chosenFile.FilePath == conflictSet.File.FilePath && !conflictSet.Conflicts.Any(x => x.FilePath == chosenFile.FilePath))
                throw new ArgumentException("chosenPath does not exist inside conflictSet");

            if (chosenFile.FilePath == conflictSet.File.FilePath)
            {
                foreach (var file in conflictSet.Conflicts)
                {
                    logger.Debug("Deleting {0}", file);
                    this.filesystemProvider.DeleteFile(file.FilePath);
                }
            }
            else
            {
                logger.Debug("Deleting {0}", conflictSet.File);
                this.filesystemProvider.DeleteFile(conflictSet.File.FilePath);

                foreach (var file in conflictSet.Conflicts)
                {
                    if (file.FilePath == chosenFile.FilePath)
                        continue;

                    logger.Debug("Deleting {0}", file);
                    this.filesystemProvider.DeleteFile(file.FilePath);
                }

                logger.Debug("Renaming {0} to {1}", chosenFile, conflictSet.File);
                this.filesystemProvider.MoveFile(chosenFile.FilePath, conflictSet.File.FilePath);
            }
        }
    }
}
