using StyletIoC;
using SyncTrayzor.SyncThing;
using SyncTrayzor.SyncThing.ApiClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public interface ISyncThingConnectionsWatcherFactory
    {
        ISyncThingConnectionsWatcher CreateConnectionsWatcher(SynchronizedTransientWrapper<ISyncThingApiClient> apiClient);
    }

    public class SyncThingConnectionsWatcherFactory : ISyncThingConnectionsWatcherFactory
    {
        public ISyncThingConnectionsWatcher CreateConnectionsWatcher(SynchronizedTransientWrapper<ISyncThingApiClient> apiClient)
        {
            return new SyncThingConnectionsWatcher(apiClient);
        }
    }
}
