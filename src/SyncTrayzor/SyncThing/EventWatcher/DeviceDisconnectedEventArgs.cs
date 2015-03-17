using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.EventWatcher
{
    public class DeviceDisconnectedEventArgs : EventArgs
    {
        public string DeviceId { get; private set; }
        public string Error { get; private set; }

        public DeviceDisconnectedEventArgs(string deviceId, string error)
        {
            this.DeviceId = deviceId;
            this.Error = error;
        }
    }
}
