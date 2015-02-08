using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public class MessageLoggedEventArgs : EventArgs
    {
        public string LogMessage { get; private set; }

        public MessageLoggedEventArgs(string logMessage)
        {
            this.LogMessage = logMessage;
        }
    }
}
