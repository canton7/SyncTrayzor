using SyncTrayzor.Syncthing.ApiClient;

namespace SyncTrayzor.Syncthing
{
    public interface ISyncthingConnectionsWatcherFactory
    {
        ISyncthingConnectionsWatcher CreateConnectionsWatcher(SynchronizedTransientWrapper<ISyncthingApiClient> apiClient);
    }

    public class SyncthingConnectionsWatcherFactory : ISyncthingConnectionsWatcherFactory
    {
        public ISyncthingConnectionsWatcher CreateConnectionsWatcher(SynchronizedTransientWrapper<ISyncthingApiClient> apiClient)
        {
            return new SyncthingConnectionsWatcher(apiClient);
        }
    }
}
