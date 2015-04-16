using Stylet;
using SyncTrayzor.SyncThing;
using SyncTrayzor.Xaml;
using SyncTrayzor.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using CefSharp;
using CefSharp.Wpf;
using SyncTrayzor.Localization;
using SyncTrayzor.Services.Config;
using System.Threading;
using SyncTrayzor.Properties;
using SyncTrayzor.Services;

namespace SyncTrayzor.Pages
{
    public class ViewerViewModel : Screen, IRequestHandler, ILifeSpanHandler
    {
        private readonly IWindowManager windowManager;
        private readonly ISyncThingManager syncThingManager;
        private readonly IProcessStartProvider processStartProvider;

        private readonly object cultureLock = new object(); // This can be read from many threads
        private CultureInfo culture;

        public string Location { get; private set; }
        
        private SyncThingState syncThingState { get; set; }
        public bool ShowSyncThingStarting { get { return this.syncThingState == SyncThingState.Starting; } }
        public bool ShowSyncThingStopped { get { return this.syncThingState == SyncThingState.Stopped; ; } }

        public IWpfWebBrowser WebBrowser { get; set; }

        private JavascriptCallbackObject callback;

        public ViewerViewModel(
            IWindowManager windowManager,
            ISyncThingManager syncThingManager,
            IConfigurationProvider configurationProvider,
            IProcessStartProvider processStartProvider)
        {
            this.windowManager = windowManager;
            this.syncThingManager = syncThingManager;
            this.processStartProvider = processStartProvider;

            this.syncThingManager.StateChanged += (o, e) =>
            {
                this.syncThingState = e.NewState;
                this.RefreshBrowser();
            };

            this.callback = new JavascriptCallbackObject(this.OpenFolder);

            this.Bind(x => x.WebBrowser, (o, e) =>
            {
                if (e.NewValue != null)
                    this.InitializeBrowser(e.NewValue);
            });

            this.SetCulture(configurationProvider.Load());
            configurationProvider.ConfigurationChanged += (o, e) => this.SetCulture(e.NewConfiguration);
        }

        private void SetCulture(Configuration configuration)
        {
            lock (this.cultureLock)
            {
                this.culture = configuration.UseComputerCulture ? Thread.CurrentThread.CurrentUICulture : null;
            }
        }

        protected override void OnInitialActivate()
        {
            Cef.Initialize(new CefSettings()
            {
                RemoteDebuggingPort = Settings.Default.CefRemoteDebuggingPort,
            });
        }

        private void InitializeBrowser(IWpfWebBrowser webBrowser)
        {
            webBrowser.RequestHandler = this;
            webBrowser.LifeSpanHandler = this;
            webBrowser.RegisterJsObject("callbackObject", this.callback);
            webBrowser.FrameLoadEnd += (o, e) =>
            {
                if (e.IsMainFrame && e.Url != "about:blank")
                {
                    var script = @"$('#folders .panel-footer .pull-right').prepend(" +
                    @"'<button class=""btn btn-sm btn-default"" onclick=""callbackObject.openFolder(angular.element(this).scope().folder.ID)"">" +
                    @"<span class=""glyphicon glyphicon-folder-open""></span>" +
                    @"<span style=""margin-left: 12px"">" +
                    Localizer.Translate("ViewerView_OpenFolder") +
                    "</span></button>')";
                    webBrowser.ExecuteScriptAsync(script);
                }
            };
        }

        public void RefreshBrowser()
        {
            this.Location = "about:blank";
            if (this.syncThingManager.State == SyncThingState.Running)
                this.Location = this.syncThingManager.Address.NormalizeZeroHost().ToString();
        }

        private void OpenFolder(string folderId)
        {
            Folder folder;
            if (!this.syncThingManager.TryFetchFolderById(folderId, out folder))
                return;

            this.processStartProvider.StartDetached("explorer.exe", folder.Path);
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

        public async void Start()
        {
            await this.syncThingManager.StartWithErrorDialogAsync(this.windowManager);
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
                this.processStartProvider.StartDetached(request.Url);
                return true;
            }

            // See https://github.com/canton7/SyncTrayzor/issues/13
            // and https://github.com/cefsharp/CefSharp/issues/534#issuecomment-60694502
            var headers = request.Headers;
            headers["X-API-Key"] = this.syncThingManager.ApiKey;
            lock (this.cultureLock)
            {
                if (this.culture != null)
                    headers["Accept-Language"] = String.Format("{0};q=0.8,en;q=0.6", this.culture.Name);
            }
            request.Headers = headers;

            return false;
        }

        bool IRequestHandler.OnCertificateError(IWebBrowser browser, CefErrorCode errorCode, string requestUrl)
        {
            // We expect cert errors from Syncthing
            return true;
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
            this.processStartProvider.StartDetached(url);
            return true;
        }

        private class JavascriptCallbackObject
        {
            private readonly Action<string> openFolderAction;

            public JavascriptCallbackObject(Action<string> openFolderAction)
	        {
                this.openFolderAction = openFolderAction;
	        }

            public void OpenFolder(string folderId)
            {
                this.openFolderAction(folderId);
            }
        }
    }
}
