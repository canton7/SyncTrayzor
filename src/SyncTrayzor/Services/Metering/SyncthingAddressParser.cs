using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SyncTrayzor.Services.Metering
{
    public static class SyncthingAddressParser
    {
        // input includes the port
        public static IPAddress Parse(string input)
        {
            // Syncthing can give us ipv6 addresses with scopes, e.g. "[fe80::21e:6ff:fea4:fdfd%Wireless Network Connection]:56478"
            // However, the scope is the name of the adapter, not the adapter's scope id (which the winapi stuff needs)
            // Therefore do some mapping...

            // Use a URI to parse off the port
            Uri uri;
            if (!Uri.TryCreate($"tcp://{input}", UriKind.Absolute, out uri))
                throw new FormatException($"Unable to parse input '{input}' into a URI");

            IPAddress ipWithoutScope;
            if (!IPAddress.TryParse(uri.Host, out ipWithoutScope))
                throw new FormatException($"Unable to parse URI host {uri.Host} into an IPAddress");

            if (ipWithoutScope.AddressFamily == AddressFamily.InterNetwork)
                return ipWithoutScope;
            else
                return ParseIPv6AddressScope(uri, ipWithoutScope);
        }

        private static IPAddress ParseIPv6AddressScope(Uri uri, IPAddress ipWithoutScope)
        {
            var idnHost = uri.DnsSafeHost; // IdnHost is preferred in .NET 4.6
            var hostWithScopeParts = idnHost.Split('%');

            // Did they provide a scope?
            IPAddress ipWithScope;
            if (hostWithScopeParts.Length > 1)
            {
                var scopeName = hostWithScopeParts[1];

                // Just in case Syncthing ever starts returning proper scope IDs...
                long scopeId;
                if (!Int64.TryParse(scopeName, out scopeId))
                {

                    var scopeLevel = ipWithoutScope.IsIPv6SiteLocal ? ScopeLevel.Site : ScopeLevel.Interface;
                    var network = NetworkInterface.GetAllNetworkInterfaces().FirstOrDefault(x => x.Name == scopeName);

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
