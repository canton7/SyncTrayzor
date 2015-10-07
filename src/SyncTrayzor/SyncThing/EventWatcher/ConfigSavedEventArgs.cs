using SyncTrayzor.SyncThing.ApiClient;
using System;

namespace SyncTrayzor.SyncThing.EventWatcher
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
