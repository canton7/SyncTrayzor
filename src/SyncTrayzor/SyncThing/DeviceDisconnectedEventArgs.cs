using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public class DeviceDisconnectedEventArgs : EventArgs
    {
        public Device Device { get; private set; }

        public DeviceDisconnectedEventArgs(Device device)
        {
            this.Device = device;
        }
    }
}
