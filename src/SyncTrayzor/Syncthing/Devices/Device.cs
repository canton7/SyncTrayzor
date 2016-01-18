using System.Net;

namespace SyncTrayzor.Syncthing.Devices
{
    public class Device
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
        }

        public Device(string deviceId, string name)
        {
            this.DeviceId = deviceId;
            this.Name = name;
        }

        public void SetConnected(IPEndPoint address)
        {
            lock (this.syncRoot)
            {
                this._address = address;
            }
        }

        public void SetDisconnected()
        {
            lock (this.syncRoot)
            {
                this._address = null;
            }
        }
    }
}
