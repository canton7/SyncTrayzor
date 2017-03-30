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

        private bool _paused;
        public bool Paused
        {
            get { lock(this.syncRoot) { return this._paused; } }
            private set { lock(this.syncRoot) { this._paused = value; } }
        }

        public Device(string deviceId, string name)
        {
            this.DeviceId = deviceId;
            this.Name = name;
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
            this.Paused = true;
        }

        public void SetResumed()
        {
            this.Paused = false;
        }

        public bool Equals(Device other)
        {
            if (ReferenceEquals(this, other))
                return true;
            if (ReferenceEquals(other, null))
                return false;

            return this.DeviceId == other.DeviceId;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Device);
        }

        public override int GetHashCode()
        {
            return this.DeviceId.GetHashCode();
        }
    }
}
