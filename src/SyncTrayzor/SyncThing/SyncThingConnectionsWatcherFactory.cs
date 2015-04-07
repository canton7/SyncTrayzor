using StyletIoC;
using SyncTrayzor.SyncThing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public interface ISyncThingConnectionsWatcherFactory
    {
        ISyncThingConnectionsWatcher CreateConnectionsWatcher(ISyncThingApiClient apiClient);
    }

    public class SyncThingConnectionsWatcherFactory : ISyncThingConnectionsWatcherFactory
    {
        public ISyncThingConnectionsWatcher CreateConnectionsWatcher(ISyncThingApiClient apiClient)
        {
            return new SyncThingConnectionsWatcher(apiClient);
        }
    }
}
