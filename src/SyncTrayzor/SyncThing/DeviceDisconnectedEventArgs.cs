using System;

namespace SyncTrayzor.SyncThing
{
    public class DeviceDisconnectedEventArgs : EventArgs
    {
        public Device Device { get; }

        public DeviceDisconnectedEventArgs(Device device)
        {
            this.Device = device;
        }
    }
}
