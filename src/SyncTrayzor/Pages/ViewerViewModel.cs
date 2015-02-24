using Stylet;
using SyncTrayzor.SyncThing;
using SyncTrayzor.Xaml;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using CefSharp;
using CefSharp.Wpf;

namespace SyncTrayzor.Pages
{
    public class ViewerViewModel : Screen, IRequestHandler, ILifeSpanHandler
    {
        private readonly ISyncThingManager syncThingManager;

        public string Location { get; private set; }
        
        private SyncThingState syncThingState { get; set; }

        public bool ShowSyncThingStarting { get { return this.syncThingState == SyncThingState.Starting; } }
        public bool ShowSyncThingStopped { get { return this.syncThingState == SyncThingState.Stopped; ; } }

        public IWpfWebBrowser WebBrowser { get; set; }

        public ViewerViewModel(ISyncThingManager syncThingManager)
        {
            this.syncThingManager = syncThingManager;
            this.syncThingManager.StateChanged += (o, e) =>
            {
                this.syncThingState = e.NewState;
                this.RefreshBrowser();
            };

            Cef.Initialize(new CefSettings());

            this.Bind(x => x.WebBrowser, (o, e) =>
            {
                if (e.NewValue == null)
                    return;

                var webBrowser = e.NewValue;
                webBrowser.RequestHandler = this;
                webBrowser.LifeSpanHandler = this;
            });
        }

        public void RefreshBrowser()
        {
            this.Location = "about:blank";
            if (this.syncThingManager.State == SyncThingState.Running && this.IsActive)
                this.Location = this.syncThingManager.Address.NormalizeZeroHost().ToString();
        }

        protected override void OnActivate()
        {
            this.RefreshBrowser();
        }

        protected override void OnDeactivate()
        {
            this.Location = "about:blank";
        }

        bool IRequestHandler.GetAuthCredentials(IWebBrowser browser, bool isProxy, string host, int port, string realm, string scheme, ref string username, ref string password)
        {
            return false;
        }

        bool IRequestHandler.OnBeforeBrowse(IWebBrowser browser, IRequest request, bool isRedirect)
        {
            return false;
        }

        bool IRequestHandler.OnBeforePluginLoad(IWebBrowser browser, string url, string policyUrl, IWebPluginInfo info)
        {
            return false;
        }

        bool IRequestHandler.OnBeforeResourceLoad(IWebBrowser browser, IRequest request, IResponse response)
        {
            var uri = new Uri(request.Url);
            if ((uri.Scheme == "http" || uri.Scheme == "https") && uri.Host != this.syncThingManager.Address.NormalizeZeroHost().Host)
            {
                Process.Start(request.Url);
                return true;
            }
            return false;
        }

        bool IRequestHandler.OnCertificateError(IWebBrowser browser, CefErrorCode errorCode, string requestUrl)
        {
            return false;
        }

        void IRequestHandler.OnPluginCrashed(IWebBrowser browser, string pluginPath)
        {
        }

        void IRequestHandler.OnRenderProcessTerminated(IWebBrowser browser, CefTerminationStatus status)
        {
        }

        void ILifeSpanHandler.OnBeforeClose(IWebBrowser browser)
        {
        }

        bool ILifeSpanHandler.OnBeforePopup(IWebBrowser browser, string url, ref int x, ref int y, ref int width, ref int height)
        {
            Process.Start(url);
            return true;
        }
    }
}
