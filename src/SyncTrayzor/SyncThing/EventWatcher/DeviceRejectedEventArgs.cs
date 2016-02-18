using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Syncthing.EventWatcher
{
    public class DeviceRejectedEventArgs : EventArgs
    {
        public string DeviceId { get; }
        public string Address { get; }

        public DeviceRejectedEventArgs(string deviceId, string address)
        {
            this.DeviceId = deviceId;
            this.Address = address;
        }
    }
}
