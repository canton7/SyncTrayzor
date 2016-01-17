using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NETWORKLIST;

namespace SyncTrayzor.Services
{
    public class MeteredNetworkManager
    {
        private NetworkListManagerClass networkListManager;

        public MeteredNetworkManager()
        {
            //var sockaddrParsed = sockaddr_in6.FromString("[fe80::21e:6ff:fea4:fdfd%2]", 56259);

            var sockAddr = new NLM_SOCKADDR() { data = new byte[128] };
            //var handle = GCHandle.Alloc(sockAddr.data, GCHandleType.Pinned);
            //try
            //{
            //    Marshal.StructureToPtr(sockaddrParsed, handle.AddrOfPinnedObject(), false);
            //}
            //finally
            //{
            //    handle.Free();
            //}

            var uri = new Uri("tcp://[fe80::21e:6ff:fea4:fdfd%Wireless Network Connection]:56259");
            var hostWithScope = uri.DnsSafeHost; // IdnHost is preferred in .NET 4.6
            var hostWithScopeParts = hostWithScope.Split('%');

            var network = NetworkInterface.GetAllNetworkInterfaces().First(x => x.Name == hostWithScopeParts[1]);
            var scopeId = network.GetIPProperties().GetIPv6Properties().GetScopeId(ScopeLevel.Interface);

            var properties = IPGlobalProperties.GetIPGlobalProperties();

            var ip = IPAddress.Parse(uri.Host);


            using (var writer = new BinaryWriter(new MemoryStream(sockAddr.data)))
            {
                // AF_INT6
                writer.Write((ushort)23);
                // Port
                writer.Write((ushort)22000);
                // Flow Info
                writer.Write((uint)0);
                // Address
                writer.Write(ip.GetAddressBytes());
                // Scope ID
                writer.Write((uint)scopeId);
            }

            this.networkListManager = new NetworkListManagerClass();
            //this.networkListManager.ConnectionCostChanged += NetworkListManager_ConnectionCostChanged;
            this.networkListManager.CostChanged += NetworkListManager_CostChanged;
            //this.networkListManager.DataPlanStatusChanged += NetworkListManager_DataPlanStatusChanged;

            //var connections = this.networkListManager.GetNetworkConnections();

            //foreach (var connection in connections.Cast<INetworkConnection>())
            //{
            //    Debug.WriteLine(connection);
            //}
            uint cost;

            // Seems to be compatible with SOCKADDR_STORAGE, which in turn is compatible with SOCKADDR_IN
            //// AF_INET
            //sockAddr.data[0] = 2;
            //sockAddr.data[1] = 0;

            //// Port?
            //sockAddr.data[2] = 0;
            //sockAddr.data[3] = 1;

            //// Addr
            //sockAddr.data[4] = 1;
            //sockAddr.data[5] = 1;
            //sockAddr.data[6] = 168;
            //sockAddr.data[7] = 192;

            //// AF_INET6
            //sockAddr.data[0] = 23;
            //sockAddr.data[1] = 0;

            //// sin6_port
            //sockAddr.data[2] = 0;
            //sockAddr.data[3] = 0;

            //// sin6_flowinfo
            //sockAddr.data[4] = 0;
            //sockAddr.data[5] = 0;
            //sockAddr.data[6] = 0;
            //sockAddr.data[7] = 0;
            //sockAddr.data[8] = 0;
            //sockAddr.data[9] = 0;
            //sockAddr.data[10] = 0;
            //sockAddr.data[11] = 0;

            //// sin6_addr (IN6_ADDR), 16 bytes
            //sockAddr.data[12] = 0xfe;
            //sockAddr.data[13] = 0x80;
            //sockAddr.data[14] = 0;
            //sockAddr.data[15] = 0;
            //sockAddr.data[16] = 0;
            //sockAddr.data[17] = 0;
            //sockAddr.data[18] = 0;
            //sockAddr.data[19] = 0;
            //sockAddr.data[20] = 0;
            //sockAddr.data[21] = 0;
            //sockAddr.data[22] = 0x02;
            //sockAddr.data[23] = 0x1e;
            //sockAddr.data[24] = 0x06;
            //sockAddr.data[25] = 0xff;
            //sockAddr.data[26] = 0xfe;
            //sockAddr.data[27] = 0xa4;


            networkListManager.GetCost(out cost, ref sockAddr);
            NLM_DATAPLAN_STATUS dataplan;
            networkListManager.GetDataPlanStatus(out dataplan, ref sockAddr);

            //networkListManager.SetDestinationAddresses(1, ref sockAddr, true);
            //networkListManager.getco
            //var networkConnection = networkListManager.GetNetworkConnection(Guid.NewGuid());
            //networkConnection.
            //var networkConnectionCost = new INetworkConnectionCost();
            //uint cost;
            //networkConnectionCost.GetCost(out cost);
        }

        private void NetworkListManager_DataPlanStatusChanged(ref NLM_SOCKADDR pDestAddr)
        {
            throw new NotImplementedException();
        }

        private void NetworkListManager_ConnectionCostChanged(Guid connectionId, uint newCost)
        {
            //throw new NotImplementedException();
        }

        private void NetworkListManager_CostChanged(uint newCost, ref NLM_SOCKADDR pDestAddr)
        {
            throw new NotImplementedException();
        }

        internal enum ADDRESS_FAMILIES : short
        {
            /// <summary>
            /// Internetwork: UDP, TCP, etc [value = 2].
            /// </summary>
            AF_INET = 2,
            /// <summary>
            /// Internetwork Version 6 [value = 23].
            /// </summary>
            AF_INET6 = 23,
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct WSAData
        {
            internal Int16 version;
            internal Int16 highVersion;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 257)]
            internal String description;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 129)]
            internal String systemStatus;

            internal Int16 maxSockets;
            internal Int16 maxUdpDg;
            internal IntPtr vendorInfo;
        }

        [StructLayout(LayoutKind.Explicit, Size = 16)]
        internal struct in6_addr
        {
            [FieldOffset(0)]
            internal byte Byte_0;
            [FieldOffset(1)]
            internal byte Byte_1;
            [FieldOffset(2)]
            internal byte Byte_2;
            [FieldOffset(3)]
            internal byte Byte_3;

            [FieldOffset(4)]
            internal byte Byte_4;
            [FieldOffset(5)]
            internal byte Byte_5;
            [FieldOffset(6)]
            internal byte Byte_6;
            [FieldOffset(7)]
            internal byte Byte_7;

            [FieldOffset(8)]
            internal byte Byte_8;
            [FieldOffset(9)]
            internal byte Byte_9;
            [FieldOffset(10)]
            internal byte Byte_10;
            [FieldOffset(11)]
            internal byte Byte_11;

            [FieldOffset(12)]
            internal byte Byte_12;
            [FieldOffset(13)]
            internal byte Byte_13;
            [FieldOffset(14)]
            internal byte Byte_14;
            [FieldOffset(15)]
            internal byte Byte_16;

            [FieldOffset(0)]
            internal short Word_0;
            [FieldOffset(2)]
            internal short Word_1;
            [FieldOffset(4)]
            internal short Word_2;
            [FieldOffset(6)]
            internal short Word_3;

            [FieldOffset(8)]
            internal short Word_4;
            [FieldOffset(10)]
            internal short Word_5;
            [FieldOffset(12)]
            internal short Word_6;
            [FieldOffset(14)]
            internal short Word_7;
        }

        [StructLayout(LayoutKind.Explicit, Size = 28)]
        internal struct sockaddr_in6
        {
            [FieldOffset(0)]
            internal ADDRESS_FAMILIES sin6_family;
            [FieldOffset(2)]
            internal ushort sin6_port;
            [FieldOffset(4)]
            internal uint sin6_flowinfo;
            [FieldOffset(8)]
            internal in6_addr sin6_addr;
            [FieldOffset(24)]
            internal uint sin6_scope_id;

            internal string Host
            {
                get
                {
                    var local = this;
                    var length = (uint)256;
                    var builder = new StringBuilder((int)length);

                    var data = new WSAData();
                    WSAStartup(2, out data);
                    WSAAddressToString(ref local, (uint)Marshal.SizeOf(local), IntPtr.Zero, builder,
                                  ref length);
                    WSACleanup();

                    return builder.ToString().Split(':')[0];
                }

            }

            internal string Port
            {
                get
                {
                    var local = this;
                    var length = (uint)256;
                    var builder = new StringBuilder((int)length);

                    var data = new WSAData();
                    WSAStartup(2, out data);
                    WSAAddressToString(ref local, (uint)Marshal.SizeOf(local), IntPtr.Zero, builder,
                                  ref length);
                    WSACleanup();

                    return builder.ToString().Split(':')[1];
                }

            }

            internal static sockaddr_in6 FromString(string host, int port)
            {
                var sockaddr = new sockaddr_in6();
                var data = new WSAData();
                var startupResult = WSAStartup(2, out data);
                if (startupResult > 0)
                {
                    var err = WSAGetLastError();
                }
                var lpAddressLength = Marshal.SizeOf(sockaddr);
                var result = WSAStringToAddress(host + ":" + port, ADDRESS_FAMILIES.AF_INET6, IntPtr.Zero,
                                  ref sockaddr, ref lpAddressLength);
                if (result > 0)
                {
                    var e = new Win32Exception();
                    var err = WSAGetLastError();
                }
                return sockaddr;
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = 28)]
        internal struct sockaddr_in6_UNSAFE
        {
            [FieldOffset(0)]
            internal ADDRESS_FAMILIES sin6_family;
            [FieldOffset(2)]
            internal ushort sin6_port;
            [FieldOffset(4)]
            internal uint sin6_flowinfo;
            [FieldOffset(8)]
            internal in6_addr sin6_addr;
            [FieldOffset(24)]
            internal uint sin6_scope_id;

            internal static sockaddr_in6 FromString(string host, int port)
            {
                var sockaddr = new sockaddr_in6();
                var lpAddressLength = Marshal.SizeOf(sockaddr);
                var result = WSAStringToAddress(host + ":" + port, ADDRESS_FAMILIES.AF_INET6, IntPtr.Zero,
                                  ref sockaddr, ref lpAddressLength);

                return sockaddr;
            }
        }

        [DllImport("ws2_32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern Int32 WSAStartup(Int16 wVersionRequested, out WSAData wsaData);

        [DllImport("Ws2_32.dll", CharSet = CharSet.Unicode, EntryPoint = "WSAAddressToStringW")]
        static extern uint WSAAddressToString(ref sockaddr_in6 lpsaAddress, uint dwAddressLength, IntPtr lpProtocolInfo, StringBuilder lpszAddressString, ref uint lpdwAddressStringLength);

        [DllImport("ws2_32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern Int32 WSACleanup();

        [DllImport("Ws2_32.dll", SetLastError = true)]
        static extern uint WSAStringToAddress(
                  string AddressString,
                  ADDRESS_FAMILIES AddressFamily,
                  IntPtr lpProtocolInfo,
                  ref sockaddr_in6 pAddr,
                  ref int lpAddressLength);

        [DllImport("ws2_32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern Int32 WSAGetLastError();
    }
}
