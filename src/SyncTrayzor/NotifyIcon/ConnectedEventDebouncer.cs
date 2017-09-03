using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SyncTrayzor.Syncthing.Devices;

namespace SyncTrayzor.NotifyIcon
{
    public interface IConnectedEventDebouncer
    {
        event EventHandler<DeviceConnectedEventArgs> DeviceConnected;

        void Connect(Device device);
        bool Disconnect(Device device);
    }

    public class ConnectedEventDebouncer : IConnectedEventDebouncer
    {
        private static readonly TimeSpan debounceTime = TimeSpan.FromSeconds(10);

        private readonly object syncRoot = new object();

        // Devices for which we've seen a connected event, but haven't yet generated a
        // Connected notification, and the CTS to cancel the timer which will signal the 
        // DeviceConnected event being fired
        private readonly Dictionary<Device, CancellationTokenSource> pendingDeviceIds = new Dictionary<Device, CancellationTokenSource>();

        public event EventHandler<DeviceConnectedEventArgs> DeviceConnected;

        public void Connect(Device device)
        {
            var cts = new CancellationTokenSource();

            lock (this.syncRoot)
            {
                if (this.pendingDeviceIds.TryGetValue(device, out var existingCts))
                {
                    // It already exists. Cancel it, restart.
                    existingCts.Cancel();
                }

                this.pendingDeviceIds[device] = cts;
            }

            this.WaitAndRaiseConnected(device, cts.Token);
        }

        private async void WaitAndRaiseConnected(Device device, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(debounceTime, cancellationToken);
            }
            catch (OperationCanceledException) { }

            bool raiseEvent = false;

            lock (this.syncRoot)
            {
                if (this.pendingDeviceIds.ContainsKey(device))
                {
                    this.pendingDeviceIds.Remove(device);
                    raiseEvent = true;
                }
            }

            if (raiseEvent)
            {
                this.DeviceConnected?.Invoke(this, new DeviceConnectedEventArgs(device));
            }
        }


        public bool Disconnect(Device device)
        {
            lock (this.syncRoot)
            {
                if (this.pendingDeviceIds.TryGetValue(device, out var cts))
                {
                    cts.Cancel();
                    this.pendingDeviceIds.Remove(device);

                    return false;
                }

                return true;
            }
        }
    }
}
