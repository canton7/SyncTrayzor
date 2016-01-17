using SyncTrayzor.Syncthing.ApiClient;

namespace SyncTrayzor.Syncthing.EventWatcher
{
    public interface ISyncthingEventWatcherFactory
    {
        ISyncthingEventWatcher CreateEventWatcher(SynchronizedTransientWrapper<ISyncthingApiClient> apiClient);
    }

    public class SyncthingEventWatcherFactory : ISyncthingEventWatcherFactory
    {
        public ISyncthingEventWatcher CreateEventWatcher(SynchronizedTransientWrapper<ISyncthingApiClient> apiClient)
        {
            return new SyncthingEventWatcher(apiClient);
        }
    }
}
