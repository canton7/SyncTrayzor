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
    public struct ConflictFile : IEquatable<ConflictFile>
    {
        public string FilePath { get; }
        public DateTime LastModified { get; }

        public ConflictFile(string filePath, DateTime lastModified)
        {
            this.FilePath = filePath;
            this.LastModified = lastModified;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ConflictFile))
                return false;
            return this.Equals((ConflictFile)obj);
        }

        public bool Equals(ConflictFile other)
        {
            return this.FilePath == other.FilePath;
        }

        public override int GetHashCode()
        {
            return this.FilePath.GetHashCode();
        }

        public override string ToString()
        {
            return this.FilePath;
        }
    }

    public struct ConflictSet
    {
        public ConflictFile File { get; }
        public List<ConflictFile> Conflicts { get; }

        public ConflictSet(ConflictFile file, List<ConflictFile> conflicts)
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
        private static readonly Regex conflictRegex = new Regex(@"^(.*).sync-conflict-(\d{8}-\d{6})(\..*)$");
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
                    var original = BaseFileNameForConflictFile(fileName);

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
                    var conflicts = kvp.Value.Select(x => new ConflictFile(x, this.filesystemProvider.GetLastWriteTime(x))).ToList();
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

        private static string BaseFileNameForConflictFile(string conflictFileName)
        {
            var parsed = conflictRegex.Match(conflictFileName);
            return parsed.Groups[1].Value + parsed.Groups[3].Value;
        }

        public void ResolveConflict(ConflictSet conflictSet, ConflictFile chosenFile)
        {
            if (chosenFile.Equals(conflictSet.File) && !conflictSet.Conflicts.Contains(chosenFile))
                throw new ArgumentException("chosenPath does not exist inside conflictSet");

            if (chosenFile.Equals(conflictSet.File))
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
                    if (file.Equals(chosenFile))
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
