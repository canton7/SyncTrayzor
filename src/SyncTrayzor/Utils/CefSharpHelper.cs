using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Utils
{
    public static class CefSharpHelper
    {
        public static void TerminateCefSharpProcess()
        {
            var executablePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "CefSharp.BrowserSubprocess.exe");
            foreach (var process in new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + Process.GetCurrentProcess().Id).Get())
            {
                if ((string)process["ExecutablePath"] == executablePath)
                {
                    try
                    {
                        Process proc = Process.GetProcessById(Convert.ToInt32(process["ProcessId"]));
                        proc.Kill();
                    }
                    catch (ArgumentException)
                    {
                        // Process already exited.
                    }
                }
            }
        }
    }
}
