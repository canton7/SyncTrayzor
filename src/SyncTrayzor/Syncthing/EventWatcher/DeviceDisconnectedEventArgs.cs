using System;

namespace SyncTrayzor.Syncthing.EventWatcher
{
    public class DeviceDisconnectedEventArgs : EventArgs
    {
        public string DeviceId { get; }
        public string Error { get; }

        public DeviceDisconnectedEventArgs(string deviceId, string error)
        {
            this.DeviceId = deviceId;
            this.Error = error;
        }
    }
}
