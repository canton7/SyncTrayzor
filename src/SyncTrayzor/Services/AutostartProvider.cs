using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SyncTrayzor.Services
{
    public class AutostartProvider
    {
        private const string applicationName = "SyncTrayzor";

        public void SetAutoStart(bool autoStart, bool startMinimized)
        {
            var registryKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            var keyExists = registryKey.GetValue(applicationName) != null;

            if (autoStart)
            {
                var path = String.Format("\"{0}\"{1}", Assembly.GetExecutingAssembly().Location, startMinimized ? " -minimized" : "");
                registryKey.SetValue(applicationName, path);
            }
            else if (keyExists)
            {
                registryKey.DeleteValue(applicationName);
            }
        }
    }
}
