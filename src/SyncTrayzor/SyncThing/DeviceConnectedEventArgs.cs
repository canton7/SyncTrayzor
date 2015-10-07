using System;

namespace SyncTrayzor.SyncThing
{
    public class DeviceConnectedEventArgs : EventArgs
    {
        public Device Device { get; }

        public DeviceConnectedEventArgs(Device device)
        {
            this.Device = device;
        }
    }
}
