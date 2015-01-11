using Stylet;
using SyncTrayzor.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages
{
    public class ShellViewModel : Screen
    {
        private readonly ISyncThingRunner syncThingRunner;

        public string ExecutablePath { get; private set; }
        public ConsoleViewModel Console { get; private set; }

        public bool IsStarted { get; private set; }

        public ShellViewModel(
            ISyncThingRunner syncThingRunner,
            ConsoleViewModel console)
        {
            this.syncThingRunner = syncThingRunner;
            this.Console = console;
        }

        public bool CanStart
        {
            get { return !this.IsStarted; }
        }
        public void Start()
        {

        }

        public bool CanStop
        {
            get { return this.IsStarted; }
        }
        public void Stop()
        {

        }
    }
}
