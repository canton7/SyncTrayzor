using System;

namespace SyncTrayzor.Syncthing
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
