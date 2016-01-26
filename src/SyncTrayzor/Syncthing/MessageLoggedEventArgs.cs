using System;

namespace SyncTrayzor.Syncthing
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
