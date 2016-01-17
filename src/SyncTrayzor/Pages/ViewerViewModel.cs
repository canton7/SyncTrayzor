using Stylet;
using SyncTrayzor.Syncthing;
using SyncTrayzor.Utils;
using System;
using System.Globalization;
using CefSharp;
using CefSharp.Wpf;
using SyncTrayzor.Services.Config;
using System.Threading;
using SyncTrayzor.Services;
using SyncTrayzor.Properties;

namespace SyncTrayzor.Pages
{
    public class ViewerViewModel : Screen, IRequestHandler, ILifeSpanHandler, IDisposable
    {
        private readonly IWindowManager windowManager;
        private readonly ISyncthingManager syncthingManager;
        private readonly IProcessStartProvider processStartProvider;
        private readonly IConfigurationProvider configurationProvider;
        private readonly IApplicationPathsProvider pathsProvider;

        private readonly object cultureLock = new object(); // This can be read from many threads
        private CultureInfo culture;
        private double zoomLevel;

        public string Location { get; private set; }
        
        private SyncthingState syncthingState { get; set; }
        public bool ShowSyncthingStarting => this.syncthingState == SyncthingState.Starting;
        public bool ShowSyncthingStopped => this.syncthingState == SyncthingState.Stopped;

        public ChromiumWebBrowser WebBrowser { get; set; }

        private JavascriptCallbackObject callback;

        public ViewerViewModel(
            IWindowManager windowManager,
            ISyncthingManager syncthingManager,
            IConfigurationProvider configurationProvider,
            IProcessStartProvider processStartProvider,
            IApplicationPathsProvider pathsProvider)
        {
            this.windowManager = windowManager;
            this.syncthingManager = syncthingManager;
            this.processStartProvider = processStartProvider;
            this.configurationProvider = configurationProvider;
            this.pathsProvider = pathsProvider;

            var configuration = this.configurationProvider.Load();
            this.zoomLevel = configuration.SyncthingWebBrowserZoomLevel;

            this.syncthingManager.StateChanged += this.SyncthingStateChanged;

            this.callback = new JavascriptCallbackObject(this.OpenFolder);

            this.Bind(x => x.WebBrowser, (o, e) =>
            {
                if (e.NewValue != null)
                    this.InitializeBrowser(e.NewValue);
            });

            this.SetCulture(configuration);
            configurationProvider.ConfigurationChanged += this.ConfigurationChanged;
        }

        private void SyncthingStateChanged(object sender, SyncthingStateChangedEventArgs e)
        {
            this.syncthingState = e.NewState;
            this.RefreshBrowser();
        }

        private void ConfigurationChanged(object sender, ConfigurationChangedEventArgs e)
        {
            this.SetCulture(e.NewConfiguration);
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
            var configuration = this.configurationProvider.Load();

            var settings = new CefSettings()
            {
                RemoteDebuggingPort = Properties.Settings.Default.CefRemoteDebuggingPort,
                // We really only want to set the LocalStorage path, but we don't have that level of control....
                CachePath = this.pathsProvider.CefCachePath,
                IgnoreCertificateErrors = true,
            };
            
            // System proxy settings (which also specify a proxy for localhost) shouldn't affect us
            settings.CefCommandLineArgs.Add("no-proxy-server", "1");

            if (configuration.DisableHardwareRendering)
            {
                settings.CefCommandLineArgs.Add("disable-gpu", "1");
                settings.CefCommandLineArgs.Add("disable-gpu-vsync", "1");
            }

            Cef.Initialize(settings);
        }

        private void InitializeBrowser(ChromiumWebBrowser webBrowser)
        {
            webBrowser.RequestHandler = this;
            webBrowser.LifeSpanHandler = this;
            webBrowser.RegisterJsObject("callbackObject", this.callback);

            // So. Fun story. From https://github.com/cefsharp/CefSharp/issues/738#issuecomment-91099199, we need to set the zoom level
            // in the FrameLoadStart event. However, the IWpfWebBrowser's ZoomLevel is a DependencyProperty, and it wraps
            // the SetZoomLevel method on the unmanaged browser (which is exposed directly by ChromiumWebBrowser, but not by IWpfWebBrowser).
            // Now, FrameLoadState and FrameLoadEnd are called on a background thread, and since ZoomLevel is a DP, it can only be changed
            // from the UI thread (it's "helpful" and does a dispatcher check for us). But, if we dispatch back to the UI thread to call
            // ZoomLevel = xxx, then CEF seems to hit threading issues, and can sometimes render things entirely badly (massive icons, no
            // localization, bad spacing, no JavaScript at all, etc).
            // So, in this case, we need to call SetZoomLevel directly, as we can do that from the thread on which FrameLoadStart is called,
            // and everything's happy.
            // However, this means that the DP value isn't updated... Which means we can't use the DP at all. We have to call SetZoomLevel
            // *everywhere*, and that means keeping a local field zoomLevel to track the current zoom level. Such is life

            webBrowser.FrameLoadStart += (o, e) => webBrowser.SetZoomLevel(this.zoomLevel);
            webBrowser.FrameLoadEnd += (o, e) =>
            {
                if (e.IsMainFrame && e.Url != "about:blank")
                {
                    var script = @"$('#folders .panel-footer .pull-right').prepend(" +
                    @"'<button class=""btn btn-sm btn-default"" onclick=""callbackObject.openFolder(angular.element(this).scope().folder.id)"">" +
                    @"<span class=""fa fa-folder-open""></span>" +
                    @"<span style=""margin-left: 3px"">" +
                    Resources.ViewerView_OpenFolder +
                    "</span></button>')";
                    webBrowser.ExecuteScriptAsync(script);
                }
            };
        }

        public void RefreshBrowser()
        {
            this.Location = "about:blank";
            if (this.syncthingManager.State == SyncthingState.Running)
                this.Location = this.syncthingManager.Address.NormalizeZeroHost().ToString();
        }

        public void ZoomIn()
        {
            this.ZoomTo(this.zoomLevel + 0.2);
        }

        public void ZoomOut()
        {
            this.ZoomTo(this.zoomLevel - 0.2);
        }

        public void ZoomReset()
        {
            this.ZoomTo(0.0);
        }

        private void ZoomTo(double zoomLevel)
        {
            if (this.WebBrowser == null || this.syncthingState != SyncthingState.Running)
                return;

            this.zoomLevel = zoomLevel;
            this.WebBrowser.SetZoomLevel(zoomLevel);
            this.configurationProvider.AtomicLoadAndSave(c => c.SyncthingWebBrowserZoomLevel = zoomLevel);
        }

        private void OpenFolder(string folderId)
        {
            Folder folder;
            if (!this.syncthingManager.Folders.TryFetchById(folderId, out folder))
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
            await this.syncthingManager.StartWithErrorDialogAsync(this.windowManager);
        }

        bool IRequestHandler.GetAuthCredentials(IWebBrowser browser, bool isProxy, string host, int port, string realm, string scheme, ref string username, ref string password)
        {
            return false;
        }

        bool IRequestHandler.OnBeforeBrowse(IWebBrowser browser, IRequest request, bool isRedirect, bool isMainFrame)
        {
            return false;
        }

        bool IRequestHandler.OnBeforePluginLoad(IWebBrowser browser, string url, string policyUrl, WebPluginInfo info)
        {
            return false;
        }

        CefReturnValue IRequestHandler.OnBeforeResourceLoad(IWebBrowser browser, IRequest request, bool isMainFrame)
        {
            var uri = new Uri(request.Url);
            // We can get http requests just after changing Syncthing's address: after we've navigated to about:blank but before navigating to
            // the new address (Which we do when Syncthing hits the 'running' State).
            // Therefore only open external browsers if Syncthing is actually running
            if (this.syncthingManager.State == SyncthingState.Running && (uri.Scheme == "http" || uri.Scheme == "https") && uri.Host != this.syncthingManager.Address.NormalizeZeroHost().Host)
            {
                this.processStartProvider.StartDetached(request.Url);
                return CefReturnValue.Cancel;
            }

            // See https://github.com/canton7/SyncTrayzor/issues/13
            // and https://github.com/cefsharp/CefSharp/issues/534#issuecomment-60694502
            var headers = request.Headers;
            headers["X-API-Key"] = this.syncthingManager.ApiKey;
            lock (this.cultureLock)
            {
                if (this.culture != null)
                    headers["Accept-Language"] = $"{this.culture.Name};q=0.8,en;q=0.6";
            }
            request.Headers = headers;

            return CefReturnValue.Continue;
        }

        bool IRequestHandler.OnCertificateError(IWebBrowser browser, CefErrorCode errorCode, string requestUrl)
        {
            // We shouldn't hit this, since IgnoreCertificateErrors is true
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

        bool ILifeSpanHandler.OnBeforePopup(IWebBrowser browser, string sourceUrl, string targetUrl, ref int x, ref int y, ref int width, ref int height)
        {
            this.processStartProvider.StartDetached(targetUrl);
            return true;
        }

        public void Dispose()
        {
            this.syncthingManager.StateChanged -= this.SyncthingStateChanged;
            this.configurationProvider.ConfigurationChanged -= this.ConfigurationChanged;
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
