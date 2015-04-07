using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public interface ISyncThingApiClientFactory
    {
        ISyncThingApiClient CreateApiClient(Uri baseAddress, string apiKey);
    }

    public class SyncThingApiClientFactory : ISyncThingApiClientFactory
    {
        public ISyncThingApiClient CreateApiClient(Uri baseAddress, string apiKey)
        {
            return new SyncThingApiClient(baseAddress, apiKey);
        }
    }
}
