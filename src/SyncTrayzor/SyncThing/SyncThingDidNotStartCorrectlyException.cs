using System;

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
