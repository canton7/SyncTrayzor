using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using SyncTrayzor.Syncthing;
using SyncTrayzor.Syncthing.Devices;

namespace SyncTrayzor.Services.Metering
{
    // Device state: Unpaused, Paused, UnpausedRenegade, PausedRenegade
    // If it's renegade, don't transition it.
    // Event decide device needs pausing: Unpaused -> Paused
    // Event decide device needs unpausing: Paused -> Unpaused
    // Event device paused: Unpaused -> PausedRenegade, UnpausedRenegade -> Paused
    // Event device resumed: Paused -> UnpausedRenegade, PausedRenegade -> Unpaused

    public interface IMeteredNetworkManager : IDisposable
    {
        event EventHandler PausedDevicesChanged;

        bool IsEnabled { get; set; }
        bool IsSupportedByWindows { get; }

        IReadOnlyList<Device> PausedDevices { get; }
    }

    public class MeteredNetworkManager : IMeteredNetworkManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly ISyncthingManager syncthingManager;
        private readonly INetworkCostManager costManager;

        public event EventHandler PausedDevicesChanged;

        private bool _isEnabled;
        public bool IsEnabled
        {
            get => this._isEnabled;
            set
            {
                if (this._isEnabled == value)
                    return;
                this._isEnabled = value;
                if (value)
                    this.Enable();
                else
                    this.Disable();
            }
        }

        public bool IsSupportedByWindows => this.costManager.IsSupported;

        private readonly object syncRoot = new object();

        public IReadOnlyList<Device> PausedDevices { get; private set; } = new List<Device>().AsReadOnly();

        private readonly Dictionary<Device, DeviceState> deviceStates = new Dictionary<Device, DeviceState>();

        public MeteredNetworkManager(ISyncthingManager syncthingManager, INetworkCostManager costManager)
        {
            this.syncthingManager = syncthingManager;
            this.costManager = costManager;

            // We won't know whether or not Syncthing supports this until it loads
            if (this.costManager.IsSupported)
            {
                this.syncthingManager.StateChanged += this.SyncthingStateChanged;
                this.syncthingManager.DataLoaded += this.DataLoaded;
                this.syncthingManager.Devices.DevicePaused += this.DevicePaused;
                this.syncthingManager.Devices.DeviceResumed += this.DeviceResumed;
                this.syncthingManager.Devices.DeviceConnected += this.DeviceConnected;
                this.syncthingManager.Devices.DeviceDisconnected += this.DeviceDisconnected;
                this.costManager.NetworkCostsChanged += this.NetworkCostsChanged;
                this.costManager.NetworksChanged += this.NetworksChanged;
            }
        }

        private void SyncthingStateChanged(object sender, SyncthingStateChangedEventArgs e)
        {
            if (e.NewState != SyncthingState.Running)
                this.ClearAllDevices();
            // Else, we'll get DataLoaded shortly
        }

        private void DataLoaded(object sender, EventArgs e)
        {
            if (!this.IsEnabled)
                return;

            this.ClearAllDevices();

            this.Update();
        }

        private void ClearAllDevices()
        {
            bool changed;
            lock (this.syncRoot)
            {
                changed = this.deviceStates.Values.Any(x => x == DeviceState.Paused);
                this.deviceStates.Clear();
            }

            if (changed)
                this.UpdatePausedDeviceIds();
        }

        private void DevicePaused(object sender, DevicePausedEventArgs e)
        {
            if (!this.IsEnabled)
                return;

            bool changed = false;
            lock (this.syncRoot)
            {
                if (!this.deviceStates.TryGetValue(e.Device, out var deviceState))
                {
                    logger.Warn($"Unable to pause device {e.Device.DeviceId} as we don't have a record of its state. This should not happen");
                    return;
                }

                if (deviceState == DeviceState.Unpaused)
                {
                    this.deviceStates[e.Device] = DeviceState.PausedRenegade;
                    logger.Debug($"Device {e.Device.DeviceId} has been paused, and has gone renegade");
                }
                else if (deviceState == DeviceState.UnpausedRenegade)
                {
                    this.deviceStates[e.Device] = DeviceState.Paused;
                    logger.Debug($"Device {e.Device.DeviceId} has been paused, and has stopped being renegade");
                    changed = true;
                }
            }

            if (changed)
                this.UpdatePausedDeviceIds();
        }

        private void DeviceResumed(object sender, DeviceResumedEventArgs e)
        {
            if (!this.IsEnabled)
                return;

            bool changed = false;
            lock (this.syncRoot)
            {
                if (!this.deviceStates.TryGetValue(e.Device, out var deviceState))
                {
                    logger.Warn($"Unable to resume device {e.Device.DeviceId} as we don't have a record of its state. This should not happen");
                    return;
                }

                if (deviceState == DeviceState.Paused)
                {
                    this.deviceStates[e.Device] = DeviceState.UnpausedRenegade;
                    logger.Debug($"Device {e.Device.DeviceId} has been resumed, and has gone renegade");
                    changed = true;
                }
                else if (deviceState == DeviceState.PausedRenegade)
                {
                    this.deviceStates[e.Device] = DeviceState.Unpaused;
                    logger.Debug($"Device {e.Device.DeviceId} has been resumed, and has stopped being renegade");
                }
            }

            if (changed)
                this.UpdatePausedDeviceIds();
        }

        private async void DeviceConnected(object sender, DeviceConnectedEventArgs e)
        {
            if (!this.IsEnabled)
                return;

            var changed = await this.UpdateDeviceAsync(e.Device);
            if (changed)
                this.UpdatePausedDeviceIds();
        }

        private void DeviceDisconnected(object sender, DeviceDisconnectedEventArgs e)
        {
            // Not sure what to do here - this is caused by the pausing. We can't unpause it otherwise
            // we'll get stuck in a cycle of connected/disconnected.
        }

        private void NetworkCostsChanged(object sender, EventArgs e)
        {
            if (!this.IsEnabled)
                return;

            logger.Debug("Network costs changed. Updating devices");
            this.ResetRenegades();
            this.Update();
        }

        private void NetworksChanged(object sender, EventArgs e)
        {
            if (!this.IsEnabled)
                return;

            logger.Debug("Networks changed. Updating devices");
            this.ResetRenegades();
            this.Update();
        }

        private void ResetRenegades()
        {
            lock (this.syncRoot)
            {
                foreach (var kvp in this.deviceStates.ToArray())
                {
                    if (kvp.Value == DeviceState.PausedRenegade)
                        this.deviceStates[kvp.Key] = DeviceState.Paused;
                    else if (kvp.Value == DeviceState.UnpausedRenegade)
                        this.deviceStates[kvp.Key] = DeviceState.Unpaused;
                }
            }
        }

        private void UpdatePausedDeviceIds()
        {
            lock (this.syncRoot)
            {
                this.PausedDevices = this.deviceStates.Where(x => x.Value == DeviceState.Paused).Select(x => x.Key).ToList().AsReadOnly();
            }

            this.PausedDevicesChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Enable()
        {
            this.Update();
        }

        private async void Disable()
        {
            List<Device> devicesToUnpause;
            lock (this.syncRoot)
            {
                devicesToUnpause = this.deviceStates.Where(x => x.Value == DeviceState.Paused).Select(x => x.Key).ToList();
            }

            this.ClearAllDevices();

            if (this.syncthingManager.State == SyncthingState.Running)
                await Task.WhenAll(devicesToUnpause.Select(x => this.syncthingManager.Devices.ResumeDeviceAsync(x)).ToList());
        }

        private async void Update()
        {
            var devices = this.syncthingManager.Devices.FetchDevices();

            // Keep device states in sync with devices
            lock (this.syncRoot)
            {
                foreach (var device in devices)
                {
                    if (!this.deviceStates.ContainsKey(device))
                        this.deviceStates[device] = device.Paused ? DeviceState.Paused : DeviceState.Unpaused;
                }
                var deviceIds = new HashSet<string>(devices.Select(x => x.DeviceId));
                foreach (var deviceState in this.deviceStates.Keys.ToList())
                {
                    if (!deviceIds.Contains(deviceState.DeviceId))
                        this.deviceStates.Remove(deviceState);
                }
            }

            var updateTasks = devices.Select(device => this.UpdateDeviceAsync(device));
            var results = await Task.WhenAll(updateTasks);

            if (results.Any())
                this.UpdatePausedDeviceIds();
        }

        private async Task<bool> UpdateDeviceAsync(Device device)
        {
            // This is called when the list of devices changes, when the network cost changes, or when a device connects
            // If the list of devices has changed, then the device won't be renegade

            if (!this.IsEnabled || this.syncthingManager.State != SyncthingState.Running || !this.syncthingManager.Capabilities.SupportsDevicePauseResume)
                return false;

            DeviceState deviceState;
            lock (this.syncRoot)
            {
                if (!this.deviceStates.TryGetValue(device, out deviceState))
                {
                    logger.Warn($"Unable to fetch device state for device ID {device.DeviceId}. This should not happen.");
                    return false;
                }
            }

            if (deviceState == DeviceState.PausedRenegade || deviceState == DeviceState.UnpausedRenegade)
            {
                logger.Debug($"Skipping update of device {device.DeviceId} as it has gone renegade");
                return false;
            }

            // The device can become disconnected at any point....
            var deviceAddress = device.Address;
            var shouldBePaused = device.IsConnected && deviceAddress != null && this.costManager.IsConnectionMetered(deviceAddress.Address);

            bool changed = false;

            if (shouldBePaused && !device.Paused)
            {
                logger.Debug($"Pausing device {device.DeviceId}");
                try
                {
                    await this.syncthingManager.Devices.PauseDeviceAsync(device);

                    lock (this.syncRoot)
                    {
                        this.deviceStates[device] = DeviceState.Paused;
                    }
                    changed = true;
                }
                catch (InvalidOperationException e)
                {
                    logger.Warn($"Could not pause device {device.DeviceId}: {e.Message}");
                }
            }
            else if (!shouldBePaused && device.Paused)
            {
                logger.Debug($"Resuming device {device.DeviceId}");
                try
                {
                    await this.syncthingManager.Devices.ResumeDeviceAsync(device);

                    lock (this.syncRoot)
                    {
                        this.deviceStates[device] = DeviceState.Unpaused;
                    }
                    changed = true;
                }
                catch (InvalidOperationException e)
                {
                    logger.Warn($"Could not resume device {device.DeviceId}: {e.Message}");
                }
            }

            return changed;
        }

        public void Dispose()
        {
            this.syncthingManager.StateChanged -= this.SyncthingStateChanged;
            this.syncthingManager.DataLoaded -= this.DataLoaded;
            this.syncthingManager.Devices.DevicePaused -= this.DevicePaused;
            this.syncthingManager.Devices.DeviceResumed -= this.DeviceResumed;
            this.syncthingManager.Devices.DeviceConnected -= this.DeviceConnected;
            this.syncthingManager.Devices.DeviceDisconnected -= this.DeviceDisconnected;
            this.costManager.NetworkCostsChanged -= this.NetworkCostsChanged;
            this.costManager.NetworksChanged -= this.NetworksChanged;
        }

        private enum DeviceState
        {
            Paused,
            Unpaused,
            PausedRenegade,
            UnpausedRenegade,
        }
    }
}
