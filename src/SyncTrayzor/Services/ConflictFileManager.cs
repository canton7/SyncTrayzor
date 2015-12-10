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
    public struct ConflictSet
    {
        public string File { get; }
        public List<string> Conflicts { get; }

        public ConflictSet(string file, List<string> conflicts)
        {
            this.File = file;
            this.Conflicts = conflicts;
        }
    }

    public interface IConflictFileManager
    {
        IObservable<ConflictSet> FindConflicts(string basePath, CancellationToken cancellationToken);
        void ResolveConflict(ConflictSet conflictSet, string chosenPath);
    }

    public class ConflictFileManager : IConflictFileManager
    {
        private const string conflictPattern = "*.sync-conflict-*";
        private static readonly Regex conflictRegex = new Regex(@"^(.*).sync-conflict-(\d{8}-\d{6})\.(.*)$");
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

                foreach (var file in this.filesystemProvider.EnumerateFiles(directory, conflictPattern, SearchOption.TopDirectoryOnly))
                {
                    var fileName = Path.GetFileName(file);
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
                    subject.Next(new ConflictSet(kvp.Key, kvp.Value));
                }

                foreach (var subDirectory in this.filesystemProvider.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly))
                {
                    if (Path.GetFileName(subDirectory) == ".stversions")
                        continue;

                    stack.Push(subDirectory);

                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        private static string BaseFileNameForConflictFile(string conflictFileName)
        {
            var parsed = conflictRegex.Match(conflictFileName);
            return parsed.Groups[1].Value + parsed.Groups[3].Value;
        }

        public void ResolveConflict(ConflictSet conflictSet, string chosenFile)
        {
            if (chosenFile != conflictSet.File && !conflictSet.Conflicts.Contains(chosenFile))
                throw new ArgumentException("chosenPath does not exist inside conflictSet");

            if (chosenFile == conflictSet.File)
            {
                foreach (var file in conflictSet.Conflicts)
                {
                    logger.Debug("Deleting {0}", file);
                    this.filesystemProvider.DeleteFile(file);
                }
            }
            else
            {
                logger.Debug("Deleting {0}", conflictSet.File);
                this.filesystemProvider.DeleteFile(conflictSet.File);

                foreach (var file in conflictSet.Conflicts)
                {
                    if (file == chosenFile)
                        continue;

                    logger.Debug("Deleting {0}", file);
                    this.filesystemProvider.DeleteFile(file);
                }

                logger.Debug("Renaming {0} to {1}", chosenFile, conflictSet.File);
                this.filesystemProvider.MoveFile(chosenFile, conflictSet.File);
            }
        }
    }
}
