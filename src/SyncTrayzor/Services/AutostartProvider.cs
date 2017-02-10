using Microsoft.Win32;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text.RegularExpressions;

namespace SyncTrayzor.Services
{
    public interface IAutostartProvider
    {
        bool IsEnabled { get; set; }
        bool CanRead { get; }
        bool CanWrite { get; }

        AutostartConfiguration GetCurrentSetup();
        void SetAutoStart(AutostartConfiguration config);
    }

    public class AutostartConfiguration
    {
        public bool AutoStart { get; set; }
        public bool StartMinimized { get; set; }

        public override string ToString()
        {
            return $"<AutostartConfiguration AutoStart={this.AutoStart} StartMinimized={this.StartMinimized}>";
        }
    }

    public class AutostartProvider : IAutostartProvider
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private const string applicationName = "SyncTrayzor";
        // Matches 'SyncTrayzor' and 'SyncTrayzor (n)' (where n is a digit)
        private static readonly Regex keyRegex = new Regex("^" + applicationName + @"(?: \((\d+)\))?$");
        private readonly string keyName;

        private readonly IAssemblyProvider assemblyProvider;

        public bool IsEnabled { get; set; }

        private bool _canRead;
        public bool CanRead => this.IsEnabled && this._canRead;

        private bool _canWrite;
        public bool CanWrite => this.IsEnabled && this._canWrite;

        public AutostartProvider(IAssemblyProvider assemblyProvider)
        {
            this.assemblyProvider = assemblyProvider;

            // Default
            this.IsEnabled = true;

            this.CheckAccess();

            // Find a key, if we can, which points to our current location
            if (this.CanRead)
                this.keyName = this.FindKeyName();
        }

        private void CheckAccess()
        {
            try
            {
                // Apparently just opening the key is not enough - there's an ACL where it will still crash when trying to write
                // to it
                using (var key = this.OpenRegistryKey(true))
                {
                    var value = key.GetValue(this.keyName);
                    key.SetValue(this.keyName, value);
                }

                this._canWrite = true;
                this._canRead = true;
                logger.Info("Have read/write access to the registry");
                return;
            }
            catch (SecurityException) { }

            try
            {
                using (var key = this.OpenRegistryKey(false))
                {
                    var value = key.GetValue(this.keyName);
                }

                this._canRead = true;
                logger.Info("Have read-only access to the registry");
                return;
            }
            catch (SecurityException) { }

            logger.Info("Have no access to the registry");
        }

        private string FindKeyName()
        {
            var numbersSeen = new List<int>();
            string foundKey = null;

            using (var key = this.OpenRegistryKey(false))
            {
                foreach (var entry in key.GetValueNames())
                {
                    var match = keyRegex.Match(entry);
                    if (match.Success)
                    {
                        // Keep a record of the highest number seen, in case we need to create a new one
                        var numberValue = match.Groups[1].Value;
                        if (numberValue == String.Empty)
                            numbersSeen.Add(1);
                        else
                            numbersSeen.Add(Int32.Parse(numberValue));

                        // See if this one points to our application
                        var keyValue = key.GetValue(entry) as string;
                        if (keyValue != null && keyValue.StartsWith($"\"{this.assemblyProvider.Location}\""))
                        {
                            foundKey = entry;
                            break;
                        }
                    }
                }
            }

            // If we've seen a key that points to our application, then that's an easy win
            // If not, find the first gap in the list of key names, and use that to create our key
            if (foundKey != null)
                return foundKey;

            // No numbers seen? "SyncTrayzor". The logic below can't handle an empty list either
            if (numbersSeen.Count == 0)
                return applicationName;

            numbersSeen.Sort();
            var firstGap = Enumerable.Range(1, numbersSeen.Count).Except(numbersSeen).FirstOrDefault();
            // Value of 0 = no gaps
            var numberToUse = firstGap == 0 ? numbersSeen[numbersSeen.Count - 1] + 1 : firstGap;

            if (numberToUse == 1)
                return applicationName;
            else
                return $"{applicationName} ({numberToUse})";
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
                var value = registryKey.GetValue(this.keyName) as string;
                if (value != null)
                {
                    autoStart = true;
                    if (value.Contains(" -minimized"))
                        startMinimized = true;
                }
            }

            var config = new AutostartConfiguration() { AutoStart = autoStart, StartMinimized = startMinimized };
            logger.Info("GetCurrentSetup determined that the current configuration is: {0}", config);
            return config;
        }

        public void SetAutoStart(AutostartConfiguration config)
        {
            if (!this.CanWrite)
                throw new InvalidOperationException("Don't have permission to write to the registry");

            logger.Info("Setting AutoStart to {0}", config);

            using (var registryKey = this.OpenRegistryKey(true))
            {
                var keyExists = registryKey.GetValue(this.keyName) != null;

                if (config.AutoStart)
                {
                    var path = String.Format("\"{0}\"{1}", this.assemblyProvider.Location, config.StartMinimized ? " -minimized" : "");
                    logger.Debug("Autostart path: {0}", path);
                    registryKey.SetValue(this.keyName, path);
                }
                else if (keyExists)
                {
                    logger.Debug("Removing pre-existing registry key");
                    registryKey.DeleteValue(this.keyName);
                }
            }
        }
    }
}
