using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public interface ISyncThingApi
    {
        [Post("/rest/shutdown")]
        Task ShutdownAsync();
    }
}
