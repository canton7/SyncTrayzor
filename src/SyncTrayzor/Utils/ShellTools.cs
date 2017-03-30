using System;
using System.Drawing;
using System.Runtime.InteropServices;
using NLog;

namespace SyncTrayzor.Utils
{
    // See http://codesdirectory.blogspot.co.uk/2013/01/displaying-system-icon-in-c-wpf.html
    public static class ShellTools
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static Icon GetIcon(string path, bool isFile)
        {
            var flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES | SHGFI_LARGEICON;
            var attribute = isFile ? FILE_ATTRIBUTE_FILE : FILE_ATTRIBUTE_DIRECTORY;
            var shfi = new SHFileInfo();
            IntPtr res;
            try
            {
                res = NativeMethods.SHGetFileInfo(path, attribute, out shfi, (uint)Marshal.SizeOf(shfi), flags);
            }
            catch (Exception e)
            {
                logger.Error(e, $"Unable to call SHGetFileInfo: {e.Message}");
                return null;
            }

            if (res == IntPtr.Zero)
            {
                logger.Error(Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error()), "SHGetFileInfo returned zero");
                return null;
            }

            try
            {
                return (Icon)Icon.FromHandle(shfi.hIcon).Clone();
            }
            catch(Exception e)
            {
                logger.Error(e, $"Failed to call Icon.FromHandle: {e.Message}");
                return null;
            }
            finally
            {
                NativeMethods.DestroyIcon(shfi.hIcon);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFileInfo
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint SHGFI_OPENICON = 0x000000002;
        private const uint SHGFI_SMALLICON = 0x000000001;
        private const uint SHGFI_LARGEICON = 0x000000000;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        private const uint FILE_ATTRIBUTE_FILE = 0x00000100;

        private static class NativeMethods
        {
            [DllImport("shell32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFileInfo psfi, uint cbFileInfo, uint uFlags);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool DestroyIcon(IntPtr hIcon);
        }
    }
}
