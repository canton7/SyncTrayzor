using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing.EventWatcher
{
    public class DeviceConnectedEventArgs : EventArgs
    {
        public string DeviceId { get; private set; }
        public string Address { get; private set; }

        public DeviceConnectedEventArgs(string deviceId, string address)
        {
            this.DeviceId = deviceId;
            this.Address = address;
        }
    }
}
