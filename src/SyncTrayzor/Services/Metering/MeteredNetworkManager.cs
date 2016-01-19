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
        bool IsEnabled { get; set; }
    }

    public class MeteredNetworkManager : IMeteredNetworkManager
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly ISyncthingManager syncthingManager;
        private readonly INetworkCostManager costManager;

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

        public MeteredNetworkManager(ISyncthingManager syncthingManager, INetworkCostManager costManager)
        {
            this.syncthingManager = syncthingManager;
            this.costManager = costManager;

            this.syncthingManager.DataLoaded += this.DataLoaded;
            this.syncthingManager.Devices.DeviceConnected += this.DeviceConnected;
            this.syncthingManager.Devices.DeviceDisconnected += this.DeviceDisconnected;
            this.costManager.NetworkCostsChanged += this.NetworkCostsChanged;
        }

        private void DataLoaded(object sender, EventArgs e)
        {
            this.Update();
        }

        private async void DeviceConnected(object sender, DeviceConnectedEventArgs e)
        {
            await this.UpdateDeviceAsync(e.Device);
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

        private async void Update()
        {
            var devices = this.syncthingManager.Devices.FetchDevices();
            foreach (var device in devices)
            {
                await UpdateDeviceAsync(device);
            }
        }

        private async Task UpdateDeviceAsync(Device device)
        {
            if (this.syncthingManager.State != SyncthingState.Running)
                return;

            var isMetered = this.IsEnabled &&
                device.IsConnected &&
                device.Address != null &&
                this.costManager.IsConnectionMetered(device.Address.Address);

            if (isMetered && device.PauseState == DevicePauseState.Unpaused)
            {
                logger.Info($"Pausing device {device.DeviceId}");
                await this.syncthingManager.Devices.PauseDeviceAsync(device.DeviceId);
            }
            else if (!isMetered && device.PauseState == DevicePauseState.PausedByUs)
            {
                logger.Info($"Resuming device {device.DeviceId}");
                await this.syncthingManager.Devices.ResumeDeviceAsync(device.DeviceId);
            }
        }

        public void Dispose()
        {
            this.syncthingManager.DataLoaded -= this.DataLoaded;
            this.syncthingManager.Devices.DeviceConnected -= this.DeviceConnected;
            this.syncthingManager.Devices.DeviceDisconnected -= this.DeviceDisconnected;
            this.costManager.NetworkCostsChanged -= this.NetworkCostsChanged;
        }
    }
}
