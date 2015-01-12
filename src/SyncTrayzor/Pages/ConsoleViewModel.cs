using Stylet;
using SyncTrayzor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages
{
    public class ConsoleViewModel : Screen
    {
        private readonly ISyncThingRunner syncThingRunner;

        public string LogMessages { get; private set; }

        public ConsoleViewModel(ISyncThingRunner syncThingRunner)
        {
            this.syncThingRunner = syncThingRunner;
            this.LogMessages = "";

            // TODO: UGLY!
            this.syncThingRunner.LogMessages.Subscribe(msg => this.LogMessages += msg + "\n");
        }
    }
}
