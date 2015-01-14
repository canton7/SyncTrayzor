using Stylet;
using SyncTrayzor.SyncThing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public ViewerViewModel Viewer { get; private set; }

        public SyncThingState SyncThingState { get; private set; }

        public ShellViewModel(
            ISyncThingManager syncThingManager,
            ConsoleViewModel console,
            ViewerViewModel viewer)
        {
            this.DisplayName = "SyncTrayzor";

            this.syncThingManager = syncThingManager;
            this.Console = console;
            this.Viewer = viewer;

            this.syncThingManager.Address = "http://localhost:4567";
            this.syncThingManager.StateChanged += (o, e) => Execute.OnUIThread(() => this.SyncThingState = e.NewState);
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
            get { return this.SyncThingState == SyncThingState.Running; }
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

        public bool CanOpenBrowser
        {
            get { return this.SyncThingState == SyncThingState.Running; }
        }
        public void OpenBrowser()
        {
            Process.Start(this.syncThingManager.Address);
        }
    }
}
