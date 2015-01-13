using Stylet;
using SyncTrayzor.SyncThing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages
{
    public class ConsoleViewModel : Screen
    {
        private readonly ISyncThingManager syncThingManager;

        public string LogMessages { get; private set; }

        public ConsoleViewModel(ISyncThingManager syncThingManager)
        {
            this.syncThingManager = syncThingManager;
            this.LogMessages = "";

            // TODO: UGLY!
            this.syncThingManager.MessageLogged += (o, e) => Execute.OnUIThread(() => this.LogMessages += e.LogMessage + "\n");
        }
    }
}
