using System;

namespace SyncTrayzor.Syncthing.Devices
{
    public class DeviceResumedEventArgs : EventArgs
    {
        public Device Device { get; }

        public DeviceResumedEventArgs(Device device)
        {
            this.Device = device;
        }
    }
}
