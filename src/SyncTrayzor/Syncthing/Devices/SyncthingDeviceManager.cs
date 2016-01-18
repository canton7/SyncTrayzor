using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SyncTrayzor.Syncthing.EventWatcher;
using SyncTrayzor.Syncthing.ApiClient;
using System.Collections.Concurrent;
using SyncTrayzor.Utils;
using NLog;
using System.Threading;

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
            this.eventWatcher.DevicePaused += this.EventDevicePaused;
            this.eventWatcher.DeviceResumed += this.EventDeviceResumed;
        }

        public bool TryFetchDeviceById(string deviceId, out Device device)
        {
            return this.devices.TryGetValue(deviceId, out device);
        }

        public IReadOnlyCollection<Device> FetchAllDevices()
        {
            return new List<Device>(this.devices.Values).AsReadOnly();
        }

        public async Task LoadDevicesAsync(Config config, CancellationToken cancellationToken)
        {
            var devices = await this.FetchDevicesAsync(config, cancellationToken);
            this.devices = new ConcurrentDictionary<string, Device>(devices.Select(x => new KeyValuePair<string, Device>(x.DeviceId, x)));
        }

        public async Task ReloadDevicesAsync(Config config, CancellationToken cancellationToken)
        {
            // Raise events as appropriate

            var devices = await this.FetchDevicesAsync(config, cancellationToken);
            var newDevices = new ConcurrentDictionary<string, Device>();
            var changeNotifications = new List<Action>();

            foreach (var device in devices)
            {
                Device existingDevice;
                if (this.devices.TryGetValue(device.DeviceId, out existingDevice))
                {
                    if (!existingDevice.IsConnected && device.IsConnected)
                        changeNotifications.Add(() => this.OnDeviceConnected(device));
                    else if (existingDevice.IsConnected && !device.IsConnected)
                        changeNotifications.Add(() => this.OnDeviceDisconnected(device));

                    // Avoid a change from PausedByUs -> PausedByUser
                    if (existingDevice.PauseState == DevicePauseState.PausedByUs && device.PauseState == DevicePauseState.PausedByUser)
                        device.SetManuallyPaused();
                }

                newDevices[device.DeviceId] = device;
            }

            this.devices = newDevices;
            foreach (var changeNotification in changeNotifications)
            {
                changeNotification();
            }
        }

        private async Task<IEnumerable<Device>> FetchDevicesAsync(Config config, CancellationToken cancellationToken)
        {
            var connections = await this.apiClient.Value.FetchConnectionsAsync(cancellationToken);
            // We can potentially see duplicate devices (if the user set their config file that way). Ignore them.
            var devices = config.Devices.DistinctBy(x => x.DeviceID).Select(device =>
            {
                var deviceObj = new Device(device.DeviceID, device.Name);
                ItemConnectionData connectionData;
                if (connections.DeviceConnections.TryGetValue(device.DeviceID, out connectionData))
                {
                    deviceObj.SetConnected(SyncthingAddressParser.Parse(connectionData.Address));
                    if (connectionData.Paused)
                        deviceObj.SetPaused();
                }
                return deviceObj;
            });

            cancellationToken.ThrowIfCancellationRequested();

            return devices;
        }
        
        public async Task PauseDeviceAsync(string deviceId)
        {
            Device device;
            if (!this.devices.TryGetValue(deviceId, out device))
                return;

            device.SetManuallyPaused();
            await this.apiClient.Value.PauseDeviceAsync(deviceId);

        }

        public async Task ResumeDeviceAsync(string deviceId)
        {
            Device device;
            if (!this.devices.TryGetValue(deviceId, out device))
                return;

            device.SetResumed();
            await this.apiClient.Value.ResumeDeviceAsync(deviceId);
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

            this.OnDeviceConnected(device);
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

            this.OnDeviceDisconnected(device);
        }

        private void EventDevicePaused(object sender, DevicePausedEventArgs e)
        {
            Device device;
            if (!this.devices.TryGetValue(e.DeviceId, out device))
            {
                logger.Warn("Unexpected device paused: {0}. It wasn't fetched when we fetched our config", e.DeviceId);
                return; // Not expecting this device! It wasn't in the config...
            }

            device.SetPaused();
        }

        private void EventDeviceResumed(object sender, DeviceResumedEventArgs e)
        {
            Device device;
            if (!this.devices.TryGetValue(e.DeviceId, out device))
            {
                logger.Warn("Unexpected device resumed: {0}. It wasn't fetched when we fetched our config", e.DeviceId);
                return; // Not expecting this device! It wasn't in the config...
            }

            device.SetResumed();
        }

        private void OnDeviceConnected(Device device)
        {
            this.eventDispatcher.Raise(this.DeviceConnected, new DeviceConnectedEventArgs(device));
        }

        private void OnDeviceDisconnected(Device device)
        {
            this.eventDispatcher.Raise(this.DeviceDisconnected, new DeviceDisconnectedEventArgs(device));
        }
    }
}
