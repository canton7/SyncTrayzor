using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public class Device
    {
        public string DeviceId { get; private set; }
        public string Name { get; private set; }

        private readonly object connectivityLock = new object();

        private bool _isConnected;
        public bool IsConnected
        {
            get { lock (this.connectivityLock) { return this._isConnected; } }
        }

        private string _address;
        public string Address
        {
            get { lock (this.connectivityLock) { return this._address; } }
        }

        public Device(string deviceId, string name)
        {
            this.DeviceId = deviceId;
            this.Name = name;
        }

        public void SetConnected(string address)
        {
            lock (this.connectivityLock)
            {
                this._isConnected = true;
                this._address = address;
            }
        }

        public void SetDisconnected()
        {
            lock (this.connectivityLock)
            {
                this._isConnected = false;
                this._address = null;
            }
        }
    }
}
