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
        bool IsSupported { get; }

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
                this.Update();
            }
        }

        public bool IsSupported => this.costManager.IsSupported && this.syncthingManager.Capabilities.SupportsDevicePauseResume;

        private readonly object pausedDeviceIdsLock = new object();
        private readonly HashSet<String> _pausedDeviceIds = new HashSet<string>();
        public IReadOnlyList<string> PausedDeviceIds { get; private set; } = new List<string>().AsReadOnly();

        public MeteredNetworkManager(ISyncthingManager syncthingManager, INetworkCostManager costManager)
        {
            this.syncthingManager = syncthingManager;
            this.costManager = costManager;

            // Only bother if it's actually supported. This stops us trying to do things like pause a device when
            // pausing isn't supported.
            if (this.IsSupported)
            {
                this.syncthingManager.DataLoaded += this.DataLoaded;
                this.syncthingManager.Devices.DeviceResumed += this.DeviceResumed;
                this.syncthingManager.Devices.DeviceConnected += this.DeviceConnected;
                this.syncthingManager.Devices.DeviceDisconnected += this.DeviceDisconnected;
                this.costManager.NetworkCostsChanged += this.NetworkCostsChanged;
                this.costManager.NetworksChanged += this.NetworksChanged;
            }
        }

        private void DataLoaded(object sender, EventArgs e)
        {
            bool changed;
            lock (this.pausedDeviceIdsLock)
            {
                changed = this._pausedDeviceIds.Count > 0;
                this._pausedDeviceIds.Clear();
            }

            if (changed)
                this.UpdatePausedDeviceIds();

            this.Update();
        }

        private void DeviceResumed(object sender, DeviceResumedEventArgs e)
        {
            bool changed;
            lock (this.pausedDeviceIdsLock)
            {
                changed = this._pausedDeviceIds.Remove(e.Device.DeviceId);
            }

            if (changed)
                this.UpdatePausedDeviceIds();
        }

        private async void DeviceConnected(object sender, DeviceConnectedEventArgs e)
        {
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
            logger.Info("Network costs changed. Updating devices");
            this.Update();
        }

        private void NetworksChanged(object sender, EventArgs e)
        {
            logger.Info("Networks changed. Updating devices");
            this.Update();
        }

        private void UpdatePausedDeviceIds()
        {
            lock (this.pausedDeviceIdsLock)
            {
                this.PausedDeviceIds = this._pausedDeviceIds.ToList().AsReadOnly();
            }

            this.PausedDevicesChanged?.Invoke(this, EventArgs.Empty);
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
            if (this.syncthingManager.State != SyncthingState.Running)
                return false;

            var isMetered = this.IsEnabled &&
                device.IsConnected &&
                device.Address != null &&
                this.costManager.IsConnectionMetered(device.Address.Address);

            bool changed = false;

            if (isMetered && device.PauseState == DevicePauseState.Unpaused)
            {
                logger.Info($"Pausing device {device.DeviceId}");
                await this.syncthingManager.Devices.PauseDeviceAsync(device.DeviceId);

                lock (this.pausedDeviceIdsLock)
                {
                    changed |= this._pausedDeviceIds.Add(device.DeviceId);
                }
            }
            else if (!isMetered && device.PauseState == DevicePauseState.PausedByUs)
            {
                logger.Info($"Resuming device {device.DeviceId}");
                await this.syncthingManager.Devices.ResumeDeviceAsync(device.DeviceId);

                lock (this.pausedDeviceIdsLock)
                {
                    changed |= this._pausedDeviceIds.Remove(device.DeviceId);
                }
            }

            return changed;
        }

        public void Dispose()
        {
            this.syncthingManager.DataLoaded -= this.DataLoaded;
            this.syncthingManager.Devices.DeviceResumed -= this.DeviceResumed;
            this.syncthingManager.Devices.DeviceConnected -= this.DeviceConnected;
            this.syncthingManager.Devices.DeviceDisconnected -= this.DeviceDisconnected;
            this.costManager.NetworkCostsChanged -= this.NetworkCostsChanged;
            this.costManager.NetworksChanged -= this.NetworksChanged;
        }
    }
}
