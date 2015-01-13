using Stylet;
using SyncTrayzor.SyncThing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Pages
{
    public class ShellViewModel : Screen
    {
        private readonly ISyncThingManager syncThingManager;

        public string ExecutablePath { get; private set; }
        public ConsoleViewModel Console { get; private set; }

        public SyncThingState SyncThingState { get; private set; }

        public ShellViewModel(
            ISyncThingManager syncThingManager,
            ConsoleViewModel console)
        {
            this.DisplayName = "SyncTrayzor";

            this.syncThingManager = syncThingManager;
            this.Console = console;

            this.syncThingManager.StateChanged += (o, e) => this.SyncThingState = e.NewState;
        }

        public bool CanStart
        {
            get { return this.SyncThingState == SyncThingState.Stopped; }
        }
        public void Start()
        {
            this.syncThingManager.ExecutablePath = "syncthing.exe"; // TEMP
            this.syncThingManager.Start();
        }

        public bool CanStop
        {
            get { return this.SyncThingState == SyncThingState.Started; }
        }
        public void Stop()
        {
            this.syncThingManager.StopAsync();
        }

        public bool CanKill
        {
            get { return this.SyncThingState != SyncThingState.Stopped; }
        }
        public void Kill()
        {
            this.syncThingManager.Kill();
        }
    }
}
