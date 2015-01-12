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
            this.DisplayName = "SyncTrayzor";

            this.syncThingRunner = syncThingRunner;
            this.Console = console;

            this.syncThingRunner.StateChanged += (o, e) => this.IsStarted = e.State == SyncThingState.Started;
        }

        public bool CanStart
        {
            get { return !this.IsStarted; }
        }
        public void Start()
        {
            this.syncThingRunner.ExecutablePath = "syncthing.exe"; // TEMP
            this.syncThingRunner.Start();
        }

        public bool CanStop
        {
            get { return this.IsStarted; }
        }
        public void Stop()
        {
            this.syncThingRunner.Kill();
        }
    }
}
