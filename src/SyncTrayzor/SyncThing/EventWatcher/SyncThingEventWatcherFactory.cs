using SyncTrayzor.SyncThing;
using SyncTrayzor.SyncThing.EventWatcher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.EventWatcher
{
    public interface ISyncThingEventWatcherFactory
    {
        ISyncThingEventWatcher CreateEventWatcher(ISyncThingApiClient apiClient);
    }

    public class SyncThingEventWatcherFactory : ISyncThingEventWatcherFactory
    {
        public ISyncThingEventWatcher CreateEventWatcher(ISyncThingApiClient apiClient)
        {
            return new SyncThingEventWatcher(apiClient);
        }
    }
}
