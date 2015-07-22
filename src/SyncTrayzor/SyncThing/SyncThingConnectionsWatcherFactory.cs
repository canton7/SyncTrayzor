using SyncTrayzor.SyncThing.ApiClient;

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
