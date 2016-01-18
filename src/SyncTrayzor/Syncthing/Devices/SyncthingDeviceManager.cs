using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SyncTrayzor.Syncthing.EventWatcher;
using SyncTrayzor.Syncthing.ApiClient;
using System.Collections.Concurrent;
using SyncTrayzor.Utils;
using NLog;

namespace SyncTrayzor.Syncthing.Devices
{
    public interface ISyncthingDeviceManager
    {
        event EventHandler<DeviceConnectedEventArgs> DeviceConnected;
        event EventHandler<DeviceDisconnectedEventArgs> DeviceDisconnected;

        bool TryFetchDeviceById(string deviceId, out Device device);
        IReadOnlyCollection<Device> FetchAllDevices();
    }

    public class SyncthingDeviceManager : ISyncthingDeviceManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly SynchronizedEventDispatcher eventDispatcher;
        private readonly SynchronizedTransientWrapper<ISyncthingApiClient> apiClient;
        private readonly ISyncthingEventWatcher eventWatcher;

        private readonly object devicesLock = new object();
        private ConcurrentDictionary<string, Device> _devices = new ConcurrentDictionary<string, Device>();
        public ConcurrentDictionary<string, Device> devices
        {
            get { lock (this.devicesLock) { return this._devices; } }
            set { lock (this.devicesLock) this._devices = value; }
        }

        public event EventHandler<DeviceConnectedEventArgs> DeviceConnected;
        public event EventHandler<DeviceDisconnectedEventArgs> DeviceDisconnected;

        public SyncthingDeviceManager(SynchronizedTransientWrapper<ISyncthingApiClient> apiClient, ISyncthingEventWatcher eventWatcher)
        {
            this.eventDispatcher = new SynchronizedEventDispatcher(this);
            this.apiClient = apiClient;
            this.eventWatcher = eventWatcher;

            this.eventWatcher.DeviceConnected += this.EventDeviceConnected;
            this.eventWatcher.DeviceDisconnected += this.EventDeviceDisconnected;
        }

        public bool TryFetchDeviceById(string deviceId, out Device device)
        {
            return this.devices.TryGetValue(deviceId, out device);
        }

        public IReadOnlyCollection<Device> FetchAllDevices()
        {
            return new List<Device>(this.devices.Values).AsReadOnly();
        }

        public async Task LoadDevicesAsync(Config config)
        {
            var connections = await this.apiClient.Value.FetchConnectionsAsync();
            // We can potentially see duplicate devices (if the user set their config file that way). Ignore them.
            var devices = config.Devices.DistinctBy(x => x.DeviceID).Select(device =>
            {
                var deviceObj = new Device(device.DeviceID, device.Name);
                ItemConnectionData connectionData;
                if (connections.DeviceConnections.TryGetValue(device.DeviceID, out connectionData))
                    deviceObj.SetConnected(SyncthingAddressParser.Parse(connectionData.Address));
                return deviceObj;
            });
            this.devices = new ConcurrentDictionary<string, Device>(devices.Select(x => new KeyValuePair<string, Device>(x.DeviceId, x)));
        }

        private void EventDeviceConnected(object sender, EventWatcher.DeviceConnectedEventArgs e)
        {
            Device device;
            if (!this.devices.TryGetValue(e.DeviceId, out device))
            {
                logger.Warn("Unexpected device connected: {0}, address {1}. It wasn't fetched when we fetched our config", e.DeviceId, e.Address);
                return; // Not expecting this device! It wasn't in the config...
            }

            device.SetConnected(SyncthingAddressParser.Parse(e.Address));

            this.eventDispatcher.Raise(this.DeviceConnected, new DeviceConnectedEventArgs(device));
        }

        private void EventDeviceDisconnected(object sender, EventWatcher.DeviceDisconnectedEventArgs e)
        {
            Device device;
            if (!this.devices.TryGetValue(e.DeviceId, out device))
            {
                logger.Warn("Unexpected device connected: {0}, error {1}. It wasn't fetched when we fetched our config", e.DeviceId, e.Error);
                return; // Not expecting this device! It wasn't in the config...
            }

            device.SetDisconnected();

            this.eventDispatcher.Raise(this.DeviceDisconnected, new DeviceDisconnectedEventArgs(device));
        }
    }
}
