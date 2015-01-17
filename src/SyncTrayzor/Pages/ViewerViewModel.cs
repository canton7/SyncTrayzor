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
        
        private bool syncThingRunning { get; set; }
        public bool ShowWebBrowser { get { return this.syncThingRunning; } }
        public bool ShowSyncThingStopped { get { return !this.syncThingRunning; } }

        public ViewerViewModel(ISyncThingManager syncThingManager)
        {
            this.syncThingManager = syncThingManager;
            this.syncThingManager.StateChanged += (o, e) =>
            {
                this.syncThingRunning = e.NewState == SyncThingState.Running || e.NewState == SyncThingState.Stopping;

                if (e.NewState == SyncThingState.Running)
                    this.Refresh();
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
