using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public class DeviceConnectedEventArgs : EventArgs
    {
        public Device Device { get; private set; }

        public DeviceConnectedEventArgs(Device device)
        {
            this.Device = device;
        }
    }
}
