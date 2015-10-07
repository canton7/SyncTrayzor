using System;

namespace SyncTrayzor.SyncThing.EventWatcher
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
