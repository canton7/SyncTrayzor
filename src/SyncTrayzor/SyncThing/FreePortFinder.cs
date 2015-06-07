using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public interface IFreePortFinder
    {
        int FindFreePort(int startingPort);
    }

    public class FreePortFinder : IFreePortFinder
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public int FindFreePort(int startingPort)
        {
            Exception lastException = null;

            for (int i = startingPort; i < 65535; i++)
            {
                try
                {
                    var listener = new TcpListener(IPAddress.Loopback, i);
                    listener.Start();
                    listener.Stop();

                    logger.Debug("Found free port: {0}", i);
                    return i;
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
                        lastException = e;
                    else
                        throw;
                }
            }

            throw lastException;
        }
    }
}
