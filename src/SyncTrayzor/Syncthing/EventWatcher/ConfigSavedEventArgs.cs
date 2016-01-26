using SyncTrayzor.Syncthing.ApiClient;
using System;

namespace SyncTrayzor.Syncthing.EventWatcher
{
    public class ConfigSavedEventArgs : EventArgs
    {
        public Config Config { get; }

        public ConfigSavedEventArgs(Config config)
        {
            this.Config = config;
        }
    }
}
