using System;

namespace SyncTrayzor.Syncthing
{
    public class SyncthingDidNotStartCorrectlyException : Exception
    {
        public SyncthingDidNotStartCorrectlyException(string message)
            : base(message)
        { }

        public SyncthingDidNotStartCorrectlyException(string message, Exception innerException)
            : base(message, innerException)
        { }
    }
}
