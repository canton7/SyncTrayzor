using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Utils
{
    public class AtomicFileStream : FileStream
    {
        private const int DefaultBufferSize = 4096;
        private const string DefaultTempFileSuffix = ".tmp";

        private readonly string path;
        private readonly string tempPath;

        public static AtomicFileStream New(string path, FileMode mode)
        {
            return New(path, TempFilePath(path), mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.Read, DefaultBufferSize, FileOptions.None);
        }

        public static AtomicFileStream New(string path, string tempFilePath, FileMode mode)
        {
            return New(path, tempFilePath, mode, (mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite), FileShare.Read, DefaultBufferSize, FileOptions.None);
        }

        public static  AtomicFileStream Bew(string path, FileMode mode, FileAccess access)
        {
            return New(path, TempFilePath(path), mode, access, FileShare.Read, DefaultBufferSize, FileOptions.None);
        }

        public static AtomicFileStream New(string path, string tempFilePath, FileMode mode, FileAccess access)
        {
            return New(path, tempFilePath, mode, access, FileShare.Read, DefaultBufferSize, FileOptions.None);
        }

        public static AtomicFileStream New(string path, FileMode mode, FileAccess access, FileShare share)
        {
            return New(path, TempFilePath(path), mode, access, share, DefaultBufferSize, FileOptions.None);
        }

        public static AtomicFileStream New(string path, string tempFilePath, FileMode mode, FileAccess access, FileShare share)
        {
            return New(path, tempFilePath, mode, access, share, DefaultBufferSize, FileOptions.None);
        }

        public static AtomicFileStream New(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize)
        {
            return New(path, TempFilePath(path), mode, access, share, bufferSize, FileOptions.None);
        }

        public static AtomicFileStream New(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync)
        {
            return New(path, TempFilePath(path), mode, access, share, bufferSize, useAsync ? FileOptions.Asynchronous : FileOptions.None);
        }

        public static AtomicFileStream New(string path, string tempFilePath, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync)
        {
            return New(path, tempFilePath, mode, access, share, bufferSize, useAsync ? FileOptions.Asynchronous : FileOptions.None);
        }

        public static AtomicFileStream New(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
        {
            return New(path, TempFilePath(path), mode, access, share, bufferSize, options);
        }

        public static AtomicFileStream New(string path, string tempPath, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);

            if (File.Exists(path) && (mode == FileMode.Append || mode == FileMode.Open || mode == FileMode.OpenOrCreate))
                File.Copy(path, tempPath);

            return new AtomicFileStream(path, tempPath, mode, access, share, bufferSize, options);
        }

        private AtomicFileStream(string path, string tempPath, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
            : base(tempPath, mode, access, share, bufferSize, options)
        {
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
