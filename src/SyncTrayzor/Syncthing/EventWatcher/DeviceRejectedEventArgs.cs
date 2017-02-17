using System;

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
