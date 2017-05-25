using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using NETWORKLIST;
using System.Runtime.InteropServices;
using NLog;

namespace SyncTrayzor.Services.Metering
{
    public interface INetworkCostManager
    {
        bool IsSupported { get; }

        event EventHandler NetworkCostsChanged;
        event EventHandler NetworksChanged;

        bool IsConnectionMetered(IPAddress address);
    }

    public class NetworkCostManager : INetworkCostManager
    {
        private const ushort AF_INET = 2;
        private const ushort AF_INET6 = 23;
        private const int sockaddrDataSize = 128;

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly NetworkListManagerClass networkListManager;

        public bool IsSupported => this.networkListManager != null;

        public event EventHandler NetworkCostsChanged;
        public event EventHandler NetworksChanged;

        public NetworkCostManager()
        {
            try
            {
                var networkListManager = new NetworkListManagerClass();
                networkListManager.ConnectionCostChanged += this.ConnectionCostChanged;
                networkListManager.NetworkConnectivityChanged += this.NetworkConnectivityChanged;

                this.networkListManager = networkListManager;
            }
            catch (COMException e) when (e.HResult == -2147220992) // 0x80040200
            {
                // Expected if we're < Windows 8
                logger.Info("Unable to load NetworkListManager: not supported");
            }
            catch (Exception e)
            {
                logger.Error(e, $"Failed to load network list manager: {e.Message}");
            }
        }

        public bool IsConnectionMetered(IPAddress address)
        {
            // < Windows 8? Never metered
            if (!this.IsSupported)
                return false;

            var sockAddr = (address.AddressFamily == AddressFamily.InterNetwork) ?
                CreateIpv4SockAddr(address) :
                CreateIPv6SockAddr(address);

            try
            {
                this.networkListManager.GetCost(out uint costVal, ref sockAddr);
                var cost = (NLM_CONNECTION_COST)costVal;
                return !cost.HasFlag(NLM_CONNECTION_COST.NLM_CONNECTION_COST_UNRESTRICTED);
            }
            catch (ArgumentException e)
            {
                // See #210
                logger.Warn(e, $"GetCost failed (ArgumentException). IP: {address}, Bytes: {BitConverter.ToString(sockAddr.data)}");
            }
            catch (COMException e)
            {
                // See #215
                logger.Warn(e, $"GetCost failed (COMException, HResult {e.HResult}). IP: {address}, Bytes: {BitConverter.ToString(sockAddr.data)}");
            }
            catch (Exception e)
            {
                // Being safe...
                logger.Warn(e, $"GetCost failed for an unknown reason. IP: {address}, Bytes: {BitConverter.ToString(sockAddr.data)}");
            }

            return false;
        }

        private static NLM_SOCKADDR CreateIpv4SockAddr(IPAddress address)
        {
            var sockAddr = new NLM_SOCKADDR() { data = new byte[sockaddrDataSize] };

            using (var writer = new BinaryWriter(new MemoryStream(sockAddr.data)))
            {
                // AF_INT
                writer.Write(AF_INET);
                // Port
                writer.Write((ushort)0);
                // Flow Info
                writer.Write((uint)0);
                // Address
                writer.Write(address.GetAddressBytes());
            }

            return sockAddr;
        }

        private static NLM_SOCKADDR CreateIPv6SockAddr(IPAddress address)
        {
            var sockAddr = new NLM_SOCKADDR() { data = new byte[sockaddrDataSize] };

            // Seems to be compatible with SOCKADDR_STORAGE, which in turn is compatible with SOCKADDR_IN6
            using (var writer = new BinaryWriter(new MemoryStream(sockAddr.data)))
            {
                // AF_INT6
                writer.Write(AF_INET6);
                // Port
                writer.Write((ushort)0);
                // Flow Info
                writer.Write((uint)0);
                // Address
                writer.Write(address.GetAddressBytes());
                // Scope ID
                writer.Write((ulong)address.ScopeId);
            }

            return sockAddr;
        }

        private void ConnectionCostChanged(Guid connectionId, uint newCost)
        {
            this.NetworkCostsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void NetworkConnectivityChanged(Guid networkId, NLM_CONNECTIVITY newConnectivity)
        {
            this.NetworksChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
