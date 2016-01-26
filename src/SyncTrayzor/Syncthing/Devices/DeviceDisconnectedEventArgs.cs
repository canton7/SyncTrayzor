using System;

namespace SyncTrayzor.Syncthing.Devices
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
