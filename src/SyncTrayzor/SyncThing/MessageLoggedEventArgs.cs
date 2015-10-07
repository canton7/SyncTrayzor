using System;

namespace SyncTrayzor.SyncThing
{
    public class MessageLoggedEventArgs : EventArgs
    {
        public string LogMessage { get; }

        public MessageLoggedEventArgs(string logMessage)
        {
            this.LogMessage = logMessage;
        }
    }
}
