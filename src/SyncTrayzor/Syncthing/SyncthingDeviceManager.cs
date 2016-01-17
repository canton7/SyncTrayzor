using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyncTrayzor.Syncthing.EventWatcher;

namespace SyncTrayzor.Syncthing
{
    public interface ISyncthingDeviceManager
    {

    }

    public class SyncthingDeviceManager : ISyncthingDeviceManager
    {
        private readonly ISyncthingEventWatcher eventWatcher;

        public SyncthingDeviceManager(ISyncthingEventWatcher eventWatcher)
        {
            this.eventWatcher = eventWatcher;
        }
    }
}
