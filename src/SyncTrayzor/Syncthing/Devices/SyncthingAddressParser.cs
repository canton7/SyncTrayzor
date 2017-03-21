using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SyncTrayzor.Syncthing.Devices
{
    public static class SyncthingAddressParser
    {
        // input includes the port
        public static IPEndPoint Parse(string input)
        {
            // Syncthing can give us ipv6 addresses with scopes, e.g. "[fe80::21e:6ff:fea4:fdfd%Wireless Network Connection]:56478"
            // However, the scope is the name of the adapter, not the adapter's scope id (which the winapi stuff needs)
            // Therefore do some mapping...

            // Use a URI to parse off the port
            if (!Uri.TryCreate($"tcp://{input}", UriKind.Absolute, out Uri uri))
                throw new FormatException($"Unable to parse input '{input}' into a URI");

            if (!IPAddress.TryParse(uri.Host, out IPAddress ipWithoutScope))
                throw new FormatException($"Unable to parse URI host {uri.Host} into an IPAddress");

            IPAddress ipWithScope;
            if (ipWithoutScope.AddressFamily == AddressFamily.InterNetwork)
                ipWithScope = ipWithoutScope;
            else
                ipWithScope = ParseIPv6AddressScope(uri, ipWithoutScope);

            return new IPEndPoint(ipWithScope, uri.Port);
        }

        private static IPAddress ParseIPv6AddressScope(Uri uri, IPAddress ipWithoutScope)
        {
            // Sometimes this can be escaped, sometimes not
            var idnHost = WebUtility.UrlDecode(uri.DnsSafeHost); // IdnHost is preferred in .NET 4.6
            var hostWithScopeParts = idnHost.Split(new[] { '%' }, 2);

            // Did they provide a scope?
            IPAddress ipWithScope;
            if (hostWithScopeParts.Length > 1)
            {
                var scopeName = hostWithScopeParts[1];

                // Just in case Syncthing ever starts returning proper scope IDs...
                if (!Int64.TryParse(scopeName, out long scopeId))
                {

                    var scopeLevel = ipWithoutScope.IsIPv6SiteLocal ? ScopeLevel.Site : ScopeLevel.Interface;
                    // I've seen Go produce ID and Name
                    var network = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(x => x.Id == scopeName || x.Name == scopeName);

                    if (network == null)
                        throw new FormatException($"Unable to find an interface with name {scopeName}");

                    scopeId = network.GetIPProperties().GetIPv6Properties().GetScopeId(scopeLevel);
                }
                ipWithScope = new IPAddress(ipWithoutScope.GetAddressBytes(), scopeId);
            }
            else
            {
                ipWithScope = ipWithoutScope;
            }

            return ipWithScope;
        }
    }
}
