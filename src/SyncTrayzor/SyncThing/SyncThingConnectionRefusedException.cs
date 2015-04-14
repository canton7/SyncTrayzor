using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public class SyncThingConnectionRefusedException : Exception
    {
        public SyncThingConnectionRefusedException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
