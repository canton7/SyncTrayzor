using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using SyncTrayzor.Syncthing;
using SyncTrayzor.Syncthing.Devices;

namespace SyncTrayzor.Services.Metering
{
    public interface IMeteredNetworkManager : IDisposable
    {
        event EventHandler PausedDevicesChanged;

        bool IsEnabled { get; set; }
        bool IsSupportedByWindows { get; }

        IReadOnlyList<string> PausedDeviceIds { get; }
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
            get { return this._isEnabled; }
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

        private readonly HashSet<string> _pausedDeviceIds = new HashSet<string>();
        public IReadOnlyList<string> PausedDeviceIds { get; private set; } = new List<string>().AsReadOnly();

        private readonly HashSet<string> renegadeDeviceIds = new HashSet<string>();

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

            bool changed;
            lock (this.syncRoot)
            {
                changed = this._pausedDeviceIds.Count > 0;
                this._pausedDeviceIds.Clear();
                this.renegadeDeviceIds.Clear();
            }

            if (changed)
                this.UpdatePausedDeviceIds();

            this.Update();
        }

        private void DevicePaused(object sender, DevicePausedEventArgs e)
        {
            if (!this.IsEnabled)
                return;

            bool changed;
            lock (this.syncRoot)
            {
                changed = this._pausedDeviceIds.Add(e.Device.DeviceId);

                // We might not have been expecting this: user has manually paused
                this.UpdateRenegadeDeviceIds(e.Device.DeviceId, changed);
            }

            if (changed)
                this.UpdatePausedDeviceIds();
        }

        private void DeviceResumed(object sender, DeviceResumedEventArgs e)
        {
            if (!this.IsEnabled)
                return;

            bool changed;
            lock (this.syncRoot)
            {
                changed = this._pausedDeviceIds.Remove(e.Device.DeviceId);

                // We might not have been expecting this: user has manually resumed
                this.UpdateRenegadeDeviceIds(e.Device.DeviceId, changed);
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

            logger.Info("Network costs changed. Updating devices");
            this.Update();
        }

        private void NetworksChanged(object sender, EventArgs e)
        {
            if (!this.IsEnabled)
                return;

            logger.Info("Networks changed. Updating devices");
            this.Update();
        }

        private void UpdateRenegadeDeviceIds(string deviceId, bool add)
        {
            // We should always be called from inside a lock, but just to be sure....
            lock (this.syncRoot)
            {
                if (add)
                {
                    if (this.renegadeDeviceIds.Add(deviceId))
                        logger.Info($"Device {deviceId} became renegade");
                }
                else
                {
                    if (this.renegadeDeviceIds.Remove(deviceId))
                        logger.Info($"Device {deviceId} stopped being renegade");
                }
            }
        }

        private void UpdatePausedDeviceIds()
        {
            lock (this.syncRoot)
            {
                this.PausedDeviceIds = this._pausedDeviceIds.ToList().AsReadOnly();
            }

            this.PausedDevicesChanged?.Invoke(this, EventArgs.Empty);
        }

        private void Enable()
        {
            this.Update();
        }

        private void ClearAllDevices()
        {
            bool changed;
            lock (this.syncRoot)
            {
                changed = this._pausedDeviceIds.Count > 0;
                this._pausedDeviceIds.Clear();
                this.renegadeDeviceIds.Clear();
            }

            if (changed)
                this.UpdatePausedDeviceIds();
        }

        private async void Disable()
        {
            List<string> deviceIdsToUnpause;
            lock (this.syncRoot)
            {
                deviceIdsToUnpause = this._pausedDeviceIds.Except(this.renegadeDeviceIds).ToList();
            }

            this.ClearAllDevices();

            if (this.syncthingManager.State == SyncthingState.Running)
                await Task.WhenAll(deviceIdsToUnpause.Select(x => this.syncthingManager.Devices.ResumeDeviceAsync(x)).ToList());
        }

        private async void Update()
        {
            var devices = this.syncthingManager.Devices.FetchDevices();
            var updateTasks = devices.Select(device => this.UpdateDeviceAsync(device));
            var results = await Task.WhenAll(updateTasks);

            if (results.Any())
                this.UpdatePausedDeviceIds();
        }

        private async Task<bool> UpdateDeviceAsync(Device device)
        {
            if (!this.IsEnabled || this.syncthingManager.State != SyncthingState.Running || !this.syncthingManager.Capabilities.SupportsDevicePauseResume)
                return false;

            lock (this.syncRoot)
            {
                if (this.renegadeDeviceIds.Contains(device.DeviceId))
                {
                    logger.Info($"Skipping update of device {device.DeviceId} as it has gone renegade");
                    return false;
                }
            }

            var shouldBePaused = device.IsConnected && this.costManager.IsConnectionMetered(device.Address.Address);

            bool changed = false;

            if (shouldBePaused && !device.Paused)
            {
                logger.Info($"Pausing device {device.DeviceId}");
                await this.syncthingManager.Devices.PauseDeviceAsync(device.DeviceId);

                lock (this.syncRoot)
                {
                    changed |= this._pausedDeviceIds.Add(device.DeviceId);
                }
            }
            else if (!shouldBePaused && device.Paused)
            {
                logger.Info($"Resuming device {device.DeviceId}");
                await this.syncthingManager.Devices.ResumeDeviceAsync(device.DeviceId);

                lock (this.syncRoot)
                {
                    changed |= this._pausedDeviceIds.Remove(device.DeviceId);
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
    }
}
