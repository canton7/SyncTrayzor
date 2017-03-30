using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SyncTrayzor.Services
{
    public interface IUserActivityMonitor
    {
        bool IsWindowFullscreen();
    }

    // Taken from http://www.richard-banks.org/2007/09/how-to-detect-if-another-application-is.html
    public class UserActivityMonitor : IUserActivityMonitor
    {
        private readonly IntPtr desktopHandle;
        private readonly IntPtr shellHandle;

        public UserActivityMonitor()
        {
            this.desktopHandle = NativeMethods.GetDesktopWindow();
            this.shellHandle = NativeMethods.GetShellWindow();
        }

        public bool IsWindowFullscreen()
        {
            bool runningFullScreen = false;

            //get the dimensions of the active window
            var hWnd = NativeMethods.GetForegroundWindow();
            if (hWnd != null && !hWnd.Equals(IntPtr.Zero))
            {
                //Check we haven't picked up the desktop or the shell
                if (!(hWnd.Equals(desktopHandle) || hWnd.Equals(shellHandle)))
                {
                    NativeMethods.GetWindowRect(hWnd, out RECT appBounds);
                    //determine if window is fullscreen
                    // TODO: Not sure if there's a nice non-winforms way of doing this
                    var screenBounds = Screen.FromHandle(hWnd).Bounds;
                    if ((appBounds.Bottom - appBounds.Top) == screenBounds.Height && (appBounds.Right - appBounds.Left) == screenBounds.Width)
                    {
                        runningFullScreen = true;
                    }
                }
            }

            return runningFullScreen;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();
            [DllImport("user32.dll")]
            public static extern IntPtr GetDesktopWindow();
            [DllImport("user32.dll")]
            public static extern IntPtr GetShellWindow();
            [DllImport("user32.dll", SetLastError = true)]
            public static extern int GetWindowRect(IntPtr hwnd, out RECT rc);
        }
    }
}
