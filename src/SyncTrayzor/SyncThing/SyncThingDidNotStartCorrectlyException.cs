using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public class SyncThingDidNotStartCorrectlyException : Exception
    {
        public SyncThingDidNotStartCorrectlyException(string message)
            : base(message)
        { }

        public SyncThingDidNotStartCorrectlyException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
