using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SyncTrayzor.Services
{
    public interface IAutostartProvider
    {
        bool CanRead { get; }
        bool CanWrite { get; }

        AutostartConfiguration GetCurrentSetup();
        void SetAutoStart(AutostartConfiguration config);
    }

    public class AutostartConfiguration
    {
        public bool AutoStart { get; set; }
        public bool StartMinimized { get; set; }
    }

    public class AutostartProvider : IAutostartProvider
    {
        private const string applicationName = "SyncTrayzor";

        public bool CanRead { get; private set; }
        public bool CanWrite { get; private set; }

        public AutostartProvider()
        {
            // Check our access
            try
            {
                this.OpenRegistryKey(true).Dispose();
                this.CanWrite = true;
                this.CanRead = true;
                return;
            }
            catch (SecurityException) { }

            try
            {
                this.OpenRegistryKey(false).Dispose();
                this.CanRead = true;
            }
            catch (SecurityException) { }
        }

        private RegistryKey OpenRegistryKey(bool writable)
        {
            return Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable);
        }

        public AutostartConfiguration GetCurrentSetup()
        {
            if (!this.CanRead)
                throw new InvalidOperationException("Don't have permission to read the registry");

            bool autoStart = false;
            bool startMinimized = false;

            using (var registryKey = this.OpenRegistryKey(false))
            {
                var value = registryKey.GetValue(applicationName) as string;
                if (value != null)
                {
                    autoStart = true;
                    if (value.Contains(" -minimized"))
                        startMinimized = true;
                }
            }

            return new AutostartConfiguration() { AutoStart = autoStart, StartMinimized = startMinimized };
        }

        public void SetAutoStart(AutostartConfiguration config)
        {
            if (!this.CanWrite)
                throw new InvalidOperationException("Don't have permission to write to the registry");

            using (var registryKey = this.OpenRegistryKey(true))
            {
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
}
