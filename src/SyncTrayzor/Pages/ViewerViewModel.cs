using Stylet;
using SyncTrayzor.SyncThing;
using SyncTrayzor.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;

namespace SyncTrayzor.Pages
{
    public class ViewerViewModel : Screen
    {
        private readonly ISyncThingManager syncThingManager;

        public string Location { get; private set; }
        
        private SyncThingState syncThingState { get; set; }

        public bool ShowWebBrowser { get { return this.syncThingState == SyncThingState.Running || this.syncThingState == SyncThingState.Stopping; } }
        public bool ShowSyncThingStarting { get { return this.syncThingState == SyncThingState.Starting; } }
        public bool ShowSyncThingStopped { get { return this.syncThingState == SyncThingState.Stopped; ; } }

        public ViewerViewModel(ISyncThingManager syncThingManager)
        {
            this.syncThingManager = syncThingManager;
            this.syncThingManager.StateChanged += (o, e) =>
            {
                this.syncThingState = e.NewState;

                if (e.NewState == SyncThingState.Running)
                    this.RefreshBrowser();
            };
        }

        public void RefreshBrowser()
        {
            this.Location = null;
            this.Location = this.syncThingManager.Address;
        }

        public void Navigating(NavigatingCancelEventArgs e)
        {
            if ((e.Uri.Scheme == "http" || e.Uri.Scheme == "https") && e.Uri != new Uri(this.syncThingManager.Address))
            {
                e.Cancel = true;
                Process.Start(e.Uri.ToString());
            }
        }

        public void ExternalWindowOpened(ExternalWindowOpenedEventArgs e)
        {
            Process.Start(e.Url);
        }
    }
}
