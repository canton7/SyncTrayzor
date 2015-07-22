using SyncTrayzor.Utils;
using System;
using System.IO;

namespace SyncTrayzor.Services
{
    public interface IFilesystemProvider
    {
        bool Exists(string path);
        FileStream Open(string path, FileMode mode);
        FileStream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare);
        FileStream OpenAtomic(string path, FileMode mode);
        FileStream OpenRead(string path);
        void Copy(string from, string to);
        void Move(string from, string to);
        void CreateDirectory(string path);
        void Delete(string path);
        void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc);
        DateTime GetLastAccessTimeUtc(string path);
        string[] GetFiles(string path);
        string ReadAllText(string path);
    }

    public class FilesystemProvider : IFilesystemProvider
    {
        public bool Exists(string path) => File.Exists(path);

        public FileStream Open(string path, FileMode mode) => File.Open(path, mode);

        public FileStream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare) => new FileStream(path, fileMode, fileAccess, fileShare);

        public FileStream OpenAtomic(string path, FileMode mode) => AtomicFileStream.Open(path, mode);

        public FileStream OpenRead(string path) => File.OpenRead(path);

        public void Copy(string from, string to) => File.Copy(from, to);

        public void Move(string from, string to) => File.Move(from, to);

        public void CreateDirectory(string path) => Directory.CreateDirectory(path);

        public void Delete(string path) =>  File.Delete(path);

        public void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) => File.SetLastAccessTimeUtc(path, lastAccessTimeUtc);

        public DateTime GetLastAccessTimeUtc(string path) => File.GetLastAccessTimeUtc(path);

        public string[] GetFiles(string path) => Directory.GetFiles(path);

        public string ReadAllText(string path) => File.ReadAllText(path);
    }
}
