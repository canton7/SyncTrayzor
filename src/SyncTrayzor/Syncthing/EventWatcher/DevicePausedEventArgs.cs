using System;

namespace SyncTrayzor.Syncthing.EventWatcher
{
    public class DevicePausedEventArgs : EventArgs
    {
        public string DeviceId { get; }

        public DevicePausedEventArgs(string deviceId)
        {
            this.DeviceId = deviceId;
        }
    }
}
