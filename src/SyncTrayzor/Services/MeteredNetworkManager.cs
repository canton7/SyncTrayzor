using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
            var sockAddr = new NLM_SOCKADDR() { data = new byte[128] };
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


            var sockaddr = sockaddr_in6.FromString("fe80::21e:6ff:fea4:fdfd", 54223);


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

                    WSAData data;
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
                var lpAddressLength = Marshal.SizeOf(sockaddr);
                WSAStringToAddress(host + ":" + port, ADDRESS_FAMILIES.AF_INET6, IntPtr.Zero,
                                  ref sockaddr, ref lpAddressLength);
                return sockaddr;
            }
        }

        internal enum ADDRESS_FAMILIES : short
        {
            /// <summary>
            /// Unspecified [value = 0].
            /// </summary>
            AF_UNSPEC = 0,
            /// <summary>
            /// Local to host (pipes, portals) [value = 1].
            /// </summary>
            AF_UNIX = 1,
            /// <summary>
            /// Internetwork: UDP, TCP, etc [value = 2].
            /// </summary>
            AF_INET = 2,
            /// <summary>
            /// Arpanet imp addresses [value = 3].
            /// </summary>
            AF_IMPLINK = 3,
            /// <summary>
            /// Pup protocols: e.g. BSP [value = 4].
            /// </summary>
            AF_PUP = 4,
            /// <summary>
            /// Mit CHAOS protocols [value = 5].
            /// </summary>
            AF_CHAOS = 5,
            /// <summary>
            /// XEROX NS protocols [value = 6].
            /// </summary>
            AF_NS = 6,
            /// <summary>
            /// IPX protocols: IPX, SPX, etc [value = 6].
            /// </summary>
            AF_IPX = 6,
            /// <summary>
            /// ISO protocols [value = 7].
            /// </summary>
            AF_ISO = 7,
            /// <summary>
            /// OSI is ISO [value = 7].
            /// </summary>
            AF_OSI = 7,
            /// <summary>
            /// european computer manufacturers [value = 8].
            /// </summary>
            AF_ECMA = 8,
            /// <summary>
            /// datakit protocols [value = 9].
            /// </summary>
            AF_DATAKIT = 9,
            /// <summary>
            /// CCITT protocols, X.25 etc [value = 10].
            /// </summary>
            AF_CCITT = 10,
            /// <summary>
            /// IBM SNA [value = 11].
            /// </summary>
            AF_SNA = 11,
            /// <summary>
            /// DECnet [value = 12].
            /// </summary>
            AF_DECnet = 12,
            /// <summary>
            /// Direct data link interface [value = 13].
            /// </summary>
            AF_DLI = 13,
            /// <summary>
            /// LAT [value = 14].
            /// </summary>
            AF_LAT = 14,
            /// <summary>
            /// NSC Hyperchannel [value = 15].
            /// </summary>
            AF_HYLINK = 15,
            /// <summary>
            /// AppleTalk [value = 16].
            /// </summary>
            AF_APPLETALK = 16,
            /// <summary>
            /// NetBios-style addresses [value = 17].
            /// </summary>
            AF_NETBIOS = 17,
            /// <summary>
            /// VoiceView [value = 18].
            /// </summary>
            AF_VOICEVIEW = 18,
            /// <summary>
            /// Protocols from Firefox [value = 19].
            /// </summary>
            AF_FIREFOX = 19,
            /// <summary>
            /// Somebody is using this! [value = 20].
            /// </summary>
            AF_UNKNOWN1 = 20,
            /// <summary>
            /// Banyan [value = 21].
            /// </summary>
            AF_BAN = 21,
            /// <summary>
            /// Native ATM Services [value = 22].
            /// </summary>
            AF_ATM = 22,
            /// <summary>
            /// Internetwork Version 6 [value = 23].
            /// </summary>
            AF_INET6 = 23,
            /// <summary>
            /// Microsoft Wolfpack [value = 24].
            /// </summary>
            AF_CLUSTER = 24,
            /// <summary>
            /// IEEE 1284.4 WG AF [value = 25].
            /// </summary>
            AF_12844 = 25,
            /// <summary>
            /// IrDA [value = 26].
            /// </summary>
            AF_IRDA = 26,
            /// <summary>
            /// Network Designers OSI &amp; gateway enabled protocols [value = 28].
            /// </summary>
            AF_NETDES = 28,
            /// <summary>
            /// [value = 29].
            /// </summary>
            AF_TCNPROCESS = 29,
            /// <summary>
            /// [value = 30].
            /// </summary>
            AF_TCNMESSAGE = 30,
            /// <summary>
            /// [value = 31].
            /// </summary>
            AF_ICLFXBM = 31
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

        [DllImport("ws2_32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern Int32 WSAStartup(Int16 wVersionRequested, out WSAData wsaData);

        [DllImport("Ws2_32.dll", CharSet = CharSet.Unicode, EntryPoint = "WSAAddressToStringW")]
        static extern uint WSAAddressToString(ref sockaddr_in6 lpsaAddress, uint dwAddressLength, IntPtr lpProtocolInfo,
        StringBuilder lpszAddressString, ref uint lpdwAddressStringLength);

        [DllImport("ws2_32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern Int32 WSACleanup();

        [DllImport("Ws2_32.dll",
              CharSet = CharSet.Unicode,
              EntryPoint = "WSAStringToAddressW")]
        static extern uint WSAStringToAddress(
                      string AddressString,
                      ADDRESS_FAMILIES AddressFamily,
                      IntPtr lpProtocolInfo,
                      ref sockaddr_in6 pAddr,
                      ref int lpAddressLength);
    }
}
