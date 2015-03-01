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

        protected override void OnClose()
        {
            // This is such a dirty, horrible, hacky thing to do...
            // So it turns out that doesn't like being shut down, then re-initialized, see http://www.magpcss.org/ceforum/viewtopic.php?f=6&t=10807&start=10
            // and others. However, if we wait a little while (presumably for the WebBrowser to die and all open connections to the subprocess
            // to close), then kill it in a very dirty way (by killing the process rather than calling Cef.Shutdown), it springs back to life
            // when Cef.Initialize is called again.
            // I'm not 100% it's not leaking something somewhere, but it seems to work, and saves 50MB of idle memory usage
            // However, I'm not comfortable enough with this to enable it permanently yet
            //await Task.Delay(5000);
            //CefSharpHelper.TerminateCefSharpProcess();
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
