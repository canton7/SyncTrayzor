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
    public class AutostartConfiguration
    {
        public bool AutoStart { get; set; }
        public bool StartMinimized { get; set; }
    }

    public class AutostartProvider
    {
        private const string applicationName = "SyncTrayzor";

        private RegistryKey OpenRegistryKey()
        {
            return Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
        }

        public AutostartConfiguration GetCurrentSetup()
        {
            bool autoStart = false;
            bool startMinimized = false;

            var registryKey = this.OpenRegistryKey();
            var value = registryKey.GetValue(applicationName) as string;
            if (value != null)
            {
                autoStart = true;
                if (value.Contains(" -minimized"))
                    startMinimized = true;
            }

            return new AutostartConfiguration() { AutoStart = autoStart, StartMinimized = startMinimized };
        }

        public void SetAutoStart(AutostartConfiguration config)
        {
            var registryKey = this.OpenRegistryKey();
            var keyExists = registryKey.GetValue(applicationName) != null;

            if (config.AutoStart)
            {
                var path = String.Format("\"{0}\"{1}", Assembly.GetExecutingAssembly().Location, config.StartMinimized ? " -minimized" : "");
                registryKey.SetValue(applicationName, path);
            }
            else if (keyExists)
            {
                registryKey.DeleteValue(applicationName);
            }
        }
    }
}
