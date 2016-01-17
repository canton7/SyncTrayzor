using System;

namespace SyncTrayzor.Syncthing.EventWatcher
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
