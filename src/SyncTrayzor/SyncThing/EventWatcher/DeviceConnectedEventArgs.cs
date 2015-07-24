using System;

namespace SyncTrayzor.SyncThing.EventWatcher
{
    public class DeviceConnectedEventArgs : EventArgs
    {
        public string DeviceId { get; }
        public string Address { get; }

        public DeviceConnectedEventArgs(string deviceId, string address)
        {
            this.DeviceId = deviceId;
            this.Address = address;
        }
    }
}
