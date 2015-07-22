using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SyncTrayzor.Utils
{
    public class AtomicFileStream : FileStream
    {
        private const int DefaultBufferSize = 4096;
        private const string DefaultTempFileSuffix = ".tmp";

        private readonly string path;
        private readonly string tempPath;

        public static AtomicFileStream Open(string path, FileMode mode)
        {
            return Open(path, TempFilePath(path), mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.Read, DefaultBufferSize, FileOptions.None);
        }

        public static AtomicFileStream Open(string path, string tempFilePath, FileMode mode)
        {
            return Open(path, tempFilePath, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.Read, DefaultBufferSize, FileOptions.None);
        }

        public static  AtomicFileStream Open(string path, FileMode mode, FileAccess access)
        {
            return Open(path, TempFilePath(path), mode, access, FileShare.Read, DefaultBufferSize, FileOptions.None);
        }

        public static AtomicFileStream Open(string path, string tempFilePath, FileMode mode, FileAccess access)
        {
            return Open(path, tempFilePath, mode, access, FileShare.Read, DefaultBufferSize, FileOptions.None);
        }

        public static AtomicFileStream Open(string path, FileMode mode, FileAccess access, FileShare share)
        {
            return Open(path, TempFilePath(path), mode, access, share, DefaultBufferSize, FileOptions.None);
        }

        public static AtomicFileStream Open(string path, string tempFilePath, FileMode mode, FileAccess access, FileShare share)
        {
            return Open(path, tempFilePath, mode, access, share, DefaultBufferSize, FileOptions.None);
        }

        public static AtomicFileStream Open(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize)
        {
            return Open(path, TempFilePath(path), mode, access, share, bufferSize, FileOptions.None);
        }

        public static AtomicFileStream Open(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync)
        {
            return Open(path, TempFilePath(path), mode, access, share, bufferSize, useAsync ? FileOptions.Asynchronous : FileOptions.None);
        }

        public static AtomicFileStream Open(string path, string tempFilePath, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync)
        {
            return Open(path, tempFilePath, mode, access, share, bufferSize, useAsync ? FileOptions.Asynchronous : FileOptions.None);
        }

        public static AtomicFileStream Open(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
        {
            return Open(path, TempFilePath(path), mode, access, share, bufferSize, options);
        }

        public static AtomicFileStream Open(string path, string tempPath, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
        {
            if (access == FileAccess.Read)
                throw new ArgumentException("If you're just opening the file for reading, AtomicFileStream won't help you at all");

            if (File.Exists(tempPath))
                File.Delete(tempPath);

            if (File.Exists(path) && (mode == FileMode.Append || mode == FileMode.Open || mode == FileMode.OpenOrCreate))
                File.Copy(path, tempPath);

            return new AtomicFileStream(path, tempPath, mode, access, share, bufferSize, options);
        }

        public static FileStream OpenWrite(string path)
        {
            return AtomicFileStream.Open(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
        }

        private AtomicFileStream(string path, string tempPath, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
            : base(tempPath, mode, access, share, bufferSize, options)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (tempPath == null)
                throw new ArgumentNullException("tempPath");

            this.path = path;
            this.tempPath = tempPath;
        }

        private static string TempFilePath(string path)
        {
            return path + DefaultTempFileSuffix;
        }

        public override void Close()
        {
            base.Close();

            bool success = NativeMethods.MoveFileEx(this.tempPath, this.path, MoveFileFlags.ReplaceExisting | MoveFileFlags.WriteThrough);
            if (!success)
                Marshal.ThrowExceptionForHR(Marshal.GetLastWin32Error());
        }

        [Flags]
        private enum MoveFileFlags
        {
            None = 0,
            ReplaceExisting = 1,
            CopyAllowed = 2,
            DelayUntilReboot = 4,
            WriteThrough = 8,
            CreateHardlink = 16,
            FailIfNotTrackable = 32,
        }

        private static class NativeMethods
        {
            [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool MoveFileEx(
                [In] string lpExistingFileName,
                [In] string lpNewFileName,
                [In] MoveFileFlags dwFlags);
        }
    }
}
