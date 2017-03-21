using System;
using System.IO;
using System.Runtime.InteropServices;

namespace SyncTrayzor.Utils
{
    public class AtomicFileStream : FileStream
    {
        private const string DefaultTempFileSuffix = ".tmp";

        private readonly string path;
        private readonly string tempPath;

        public AtomicFileStream(string path)
            : this(path, TempFilePath(path))
        {
        }

        public AtomicFileStream(string path, string tempPath)
            : base(tempPath, FileMode.Create, FileAccess.ReadWrite)
        {
            this.path = path ?? throw new ArgumentNullException("path");
            this.tempPath = tempPath ?? throw new ArgumentNullException("tempPath");
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
