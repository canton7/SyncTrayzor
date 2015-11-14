using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NETWORKLIST;

namespace SyncTrayzor.Services
{
    public class MeteredNetworkManager
    {
        public MeteredNetworkManager()
        {
            var networkListManager = new NetworkListManagerClass();
            uint cost;
            var sockAddr = new NLM_SOCKADDR() { data = new byte[]{ 0 } };
            networkListManager.GetCost(out cost, ref sockAddr);
            //networkListManager.getco
            //var networkConnection = networkListManager.GetNetworkConnection(Guid.NewGuid());
            //networkConnection.
            //var networkConnectionCost = new INetworkConnectionCost();
            //uint cost;
            //networkConnectionCost.GetCost(out cost);
        }
    }
}
