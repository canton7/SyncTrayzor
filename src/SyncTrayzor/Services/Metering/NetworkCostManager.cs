using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using NETWORKLIST;

namespace SyncTrayzor.Services.Metering
{
    public class NetworkCostManager
    {
        private const ushort AF_INET = 2;
        private const ushort AF_INET6 = 23;
        private const int sockaddrDataSize = 128;

        private readonly NetworkListManagerClass networkListManager;

        public event EventHandler NetworkCostsChanged;

        public NetworkCostManager()
        {
            this.networkListManager = new NetworkListManagerClass();
            this.networkListManager.ConnectionCostChanged += this.ConnectionCostChanged;
        }

        public bool IsConnectionMetered(IPAddress address)
        {
            var sockAddr = (address.AddressFamily == AddressFamily.InterNetwork) ?
                CreateIpv4SockAddr(address) :
                CreateIPv6SockAddr(address);

            uint costVal;
            this.networkListManager.GetCost(out costVal, ref sockAddr);

            var cost = (NLM_CONNECTION_COST)costVal;

            return !cost.HasFlag(NLM_CONNECTION_COST.NLM_CONNECTION_COST_UNRESTRICTED);
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
    }
}
