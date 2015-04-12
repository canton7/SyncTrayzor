using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Services
{
    public interface IFilesystemProvider
    {
        bool Exists(string path);
        FileStream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare);
        void Move(string from, string to);
        void CreateDirectory(string path);
        void Delete(string path);
    }

    public class FilesystemProvider : IFilesystemProvider
    {
        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        public FileStream Open(string path, FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
        {
            return new FileStream(path, fileMode, fileAccess, fileShare);
        }

        public void Move(string from, string to)
        {
            File.Move(from, to);
        }

        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        public void Delete(string path)
        {
            File.Delete(path);
        }
    }
}
