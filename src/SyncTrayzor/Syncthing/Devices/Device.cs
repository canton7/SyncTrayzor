using System;
using System.Net;

namespace SyncTrayzor.Syncthing.Devices
{
    public class Device : IEquatable<Device>
    {
        private readonly object syncRoot = new object();

        public string DeviceId { get; }
        public string Name { get; }

        public bool IsConnected
        {
            get { lock (this.syncRoot) { return this._address != null; } }
        }

        private IPEndPoint _address;
        public IPEndPoint Address
        {
            get { lock (this.syncRoot) { return this._address; } }
            private set { lock(this.syncRoot) { this._address = value; } }
        }

        private DevicePauseState _pauseState;
        public DevicePauseState PauseState
        {
            get { lock(this.syncRoot) { return this._pauseState; } }
            private set { lock(this.syncRoot) { this._pauseState = value; } }
        }

        public Device(string deviceId, string name)
        {
            this.DeviceId = deviceId;
            this.Name = name;
            this.PauseState = DevicePauseState.Unpaused;
        }

        public void SetManuallyPaused()
        {
            this.PauseState = DevicePauseState.PausedByUs;
        }

        public void SetConnected(IPEndPoint address)
        {
            this.Address = address;
        }

        public void SetDisconnected()
        {
            this.Address = null;
        }

        public void SetPaused()
        {
            if (this.PauseState != DevicePauseState.PausedByUs)
                this.PauseState = DevicePauseState.PausedByUser;
        }

        public void SetResumed()
        {
            this.PauseState = DevicePauseState.Unpaused;
        }

        public bool Equals(Device other)
        {
            if (Object.ReferenceEquals(this, other))
                return true;
            if (Object.ReferenceEquals(other, null))
                return false;

            return this.DeviceId == other.DeviceId;
        }
    }
}
