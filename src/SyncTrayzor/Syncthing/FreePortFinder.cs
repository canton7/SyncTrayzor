using NLog;
using System;
using System.Net;
using System.Net.Sockets;

namespace SyncTrayzor.Syncthing
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
                    lastException = e;
                }
            }

            throw lastException;
        }
    }
}
