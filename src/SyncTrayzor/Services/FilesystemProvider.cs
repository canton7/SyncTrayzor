using SyncTrayzor.Utils;
using System;
using System.IO;
using System.Collections.Generic;

namespace SyncTrayzor.Services
{
    public interface IFilesystemProvider
    {
        bool FileExists(string path);
        bool DirectoryExists(string path);
        FileStream Open(string path, FileMode mode);
        FileStream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare);
        FileStream CreateAtomic(string path);
        FileStream OpenRead(string path);
        void Copy(string from, string to);
        void MoveFile(string from, string to);
        void CreateDirectory(string path);
        void DeleteFile(string path);
        void DeleteFileToRecycleBin(string path);
        void DeleteDirectory(string path, bool recursive);
        void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc);
        DateTime GetLastAccessTimeUtc(string path);
        DateTime GetLastWriteTime(string path);
        string[] GetFiles(string path);
        string ReadAllText(string path);
        string[] GetFiles(string path, string searchPattern, SearchOption searchOption);
        string[] GetDirectories(string path);
        string[] GetDirectories(string path, string searchPattern, SearchOption searchOption);
        IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);
        IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption);
    }

    public class FilesystemProvider : IFilesystemProvider
    {
        private const int maxPath = 260;

        public bool FileExists(string path) => Pri.LongPath.File.Exists(path);

        public bool DirectoryExists(string path) => Pri.LongPath.Directory.Exists(path);

        public FileStream Open(string path, FileMode mode) => Pri.LongPath.File.Open(path, mode);

        public FileStream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare) => new FileStream(path, fileMode, fileAccess, fileShare);

        public FileStream CreateAtomic(string path) => new AtomicFileStream(path);

        public FileStream OpenRead(string path) => Pri.LongPath.File.OpenRead(path);

        public void Copy(string from, string to) => Pri.LongPath.File.Copy(from, to);

        public void MoveFile(string from, string to) => Pri.LongPath.File.Move(from, to);

        public void CreateDirectory(string path) => Pri.LongPath.Directory.CreateDirectory(path);

        public void DeleteFile(string path) => Pri.LongPath.File.Delete(path);

        public void DeleteFileToRecycleBin(string path)
        {
            // This won't work with paths > MAX_PATH
            if (path.Length >= maxPath)
                Pri.LongPath.File.Delete(path);
            else
                RecycleBinDeleter.Delete(path);
        }

        public void DeleteDirectory(string path, bool recursive) => Pri.LongPath.Directory.Delete(path, recursive);

        public void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) => Pri.LongPath.File.SetLastAccessTimeUtc(path, lastAccessTimeUtc);

        public DateTime GetLastAccessTimeUtc(string path) => Pri.LongPath.File.GetLastAccessTimeUtc(path);

        public DateTime GetLastWriteTime(string path) => Pri.LongPath.File.GetLastWriteTime(path);

        public string[] GetFiles(string path) => Pri.LongPath.Directory.GetFiles(path);

        public string ReadAllText(string path) => Pri.LongPath.File.ReadAllText(path);

        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption) => Pri.LongPath.Directory.GetFiles(path, searchPattern, searchOption);

        public string[] GetDirectories(string path) => Pri.LongPath.Directory.GetDirectories(path);

        public string[] GetDirectories(string path, string searchPattern, SearchOption searchOption) => Pri.LongPath.Directory.GetDirectories(path, searchPattern, searchOption);

        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
            => Pri.LongPath.Directory.EnumerateFiles(path, searchPattern, searchOption);

        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption)
            => Pri.LongPath.Directory.EnumerateDirectories(path, searchPattern, searchOption);
    }
}
