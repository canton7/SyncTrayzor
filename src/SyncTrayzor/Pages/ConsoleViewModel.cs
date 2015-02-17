using Stylet;
using SyncTrayzor.SyncThing;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages
{
    public class ConsoleViewModel : Screen
    {
        private const int maxLogMessages = 500;

        private readonly ISyncThingManager syncThingManager;

        public ObservableQueue<string> LogMessages { get; private set; }

        public ConsoleViewModel(ISyncThingManager syncThingManager)
        {
            this.syncThingManager = syncThingManager;
            this.LogMessages = new ObservableQueue<string>();

            this.syncThingManager.MessageLogged += (o, e) =>
            {
                this.LogMessages.Enqueue(e.LogMessage);
                if (this.LogMessages.Count > maxLogMessages)
                    this.LogMessages.Dequeue();
            };
        }
    }
}
