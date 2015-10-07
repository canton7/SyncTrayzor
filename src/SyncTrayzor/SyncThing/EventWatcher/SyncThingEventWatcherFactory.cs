using SyncTrayzor.SyncThing.ApiClient;

namespace SyncTrayzor.SyncThing.EventWatcher
{
    public interface ISyncThingEventWatcherFactory
    {
        ISyncThingEventWatcher CreateEventWatcher(SynchronizedTransientWrapper<ISyncThingApiClient> apiClient);
    }

    public class SyncThingEventWatcherFactory : ISyncThingEventWatcherFactory
    {
        public ISyncThingEventWatcher CreateEventWatcher(SynchronizedTransientWrapper<ISyncThingApiClient> apiClient)
        {
            return new SyncThingEventWatcher(apiClient);
        }
    }
}
