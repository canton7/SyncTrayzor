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

using Microsoft.WindowsAPICodePack.Dialogs;

namespace SyncTrayzor.Pages
{
    public class ViewerViewModel : Screen, IRequestHandler, ILifeSpanHandler, IContextMenuHandler, IDisposable
    {
        private readonly IWindowManager windowManager;
        private readonly ISyncthingManager syncthingManager;
        private readonly IProcessStartProvider processStartProvider;
        private readonly IConfigurationProvider configurationProvider;
        private readonly IApplicationPathsProvider pathsProvider;

        private readonly object cultureLock = new object(); // This can be read from many threads
        private CultureInfo culture;
        private double zoomLevel;

        public string Location
        {
            get => this.WebBrowser?.Address;
            private set
            {
                if (this.WebBrowser != null)
                    this.WebBrowser.Address = value;
            }
        }
        
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

            this.callback = new JavascriptCallbackObject(this.OpenFolder, this.BrowseFolderPath);

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
            if (!Cef.IsInitialized)
            {
                var configuration = this.configurationProvider.Load();

                var settings = new CefSettings()
                {
                    RemoteDebuggingPort = AppSettings.Instance.CefRemoteDebuggingPort,
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
                    settings.CefCommandLineArgs.Add("disable-cache", "1");
                    settings.CefCommandLineArgs.Add("disable-application-cache", "1");
                }

                Cef.Initialize(settings);
            }

            var webBrowser = new ChromiumWebBrowser();
            this.InitializeBrowser(webBrowser);
            this.WebBrowser = webBrowser;
            this.RefreshBrowser();
        }

        private void InitializeBrowser(ChromiumWebBrowser webBrowser)
        {
            webBrowser.RequestHandler = this;
            webBrowser.LifeSpanHandler = this;
            webBrowser.MenuHandler = this;
            // Don't enable touch scrolling yet - it's still buggy, and causes tapping on links to fail
            //webBrowser.IsManipulationEnabled = true;
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
                if (e.Frame.IsMain && e.Url != "about:blank")
                {
                    // I tried to do this using Syncthing's events, but it's very painful - the DOM is updated some time
                    // after the event is fired. It's a lot easier to just watch for changes on the DOM.
                    var addOpenFolderButton =
                    @"var syncTrayzorAddOpenFolderButton = function(elem) {" +
                    @"    var $buttonContainer = elem.find('.panel-footer .pull-right');" +
                    @"    $buttonContainer.find('.panel-footer .synctrayzor-add-folder-button').remove();" +
                    @"    $buttonContainer.prepend(" +
                    @"      '<button class=""btn btn-sm btn-default synctrayzor-add-folder-button"" onclick=""callbackObject.openFolder(angular.element(this).scope().folder.id)"">" +
                    @"          <span class=""fa fa-folder-open""></span>" +
                    @"          <span style=""margin-left: 3px"">" + Resources.ViewerView_OpenFolder + @"</span>" +
                    @"      </button>');" +
                    @"};" +
                    @"new MutationObserver(function(mutations, observer) {" +
                    @"  for (var i = 0; i < mutations.length; i++) {" +
                    @"    for (var j = 0; j < mutations[i].addedNodes.length; j++) {" +
                    @"      syncTrayzorAddOpenFolderButton($(mutations[i].addedNodes[j]));" +
                    @"    }" +
                    @"  }" +
                    @"}).observe(document.getElementById('folders'), {" +
                    @"  childList: true" +
                    @"});" +
                    @"syncTrayzorAddOpenFolderButton($('#folders'));" +
                    @"";
                    webBrowser.ExecuteScriptAsync(addOpenFolderButton);

                    var addFolderBrowse =
                    @"$('#folderPath').wrap($('<div/>').css('display', 'flex'));" +
                    @"$('#folderPath').after(" +
                    @"  $('<button>').attr('id', 'folderPathBrowseButton')" +
                    @"               .addClass('btn btn-sm btn-default')" +
                    @"               .html('" + Resources.ViewerView_BrowseToFolder + @"')" +
                    @"               .css({'flex-grow': 1, 'margin': '0 0 0 5px'})" +
                    @"               .on('click', function() { callbackObject.browseFolderPath() })" +
                    @");" +
                    @"$('#folderPath').removeAttr('list');" +
                    @"$('#directory-list').remove();" +
                    @"$('#editFolder').on('shown.bs.modal', function() {" +
                    @"  if ($('#folderPath').is('[readonly]')) {" +
                    @"      $('#folderPathBrowseButton').attr('disabled', 'disabled');" +
                    @"  }" +
                    @"  else {" +
                    @"      $('#folderPathBrowseButton').removeAttr('disabled');" +
                    @"  }" +
                    @"});";
                    webBrowser.ExecuteScriptAsync(addFolderBrowse);
                }
            };

            // Chinese IME workaround, copied from
            // https://github.com/cefsharp/CefSharp/commit/c7c90581da7ed3dda80fd8304a856462d133d9a7
            webBrowser.PreviewTextInput += (o, e) =>
            {
                var host = webBrowser.GetBrowser().GetHost();
                var keyEvent = new KeyEvent();
                foreach (var character in e.Text)
                {
                    keyEvent.WindowsKeyCode = character;
                    keyEvent.Type = KeyEventType.Char;
                    host.SendKeyEvent(keyEvent);
                }
                e.Handled = true;
            };
        }

        public void RefreshBrowserNukeCache()
        {
            if (this.Location == this.GetSyncthingAddress().ToString())
            {
                this.WebBrowser?.Reload(ignoreCache: true);
            }
            else if (this.syncthingManager.State == SyncthingState.Running)
            {
                this.Location = this.GetSyncthingAddress().ToString();
            }
        }

        public void RefreshBrowser()
        {
            this.Location = "about:blank";
            if (this.syncthingManager.State == SyncthingState.Running)
            {
                this.Location = this.GetSyncthingAddress().ToString();
            }
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
            if (!this.syncthingManager.Folders.TryFetchById(folderId, out var folder))
                return;

            this.processStartProvider.ShowFolderInExplorer(folder.Path);
        }

        private void BrowseFolderPath()
        {
            Execute.OnUIThread(() =>
            {
                var dialog = new CommonOpenFileDialog()
                {
                    IsFolderPicker = true,
                };
                var result = dialog.ShowDialog();
                if (result == CommonFileDialogResult.Ok)
                {
                    var script =
                    @"$('#folderPath').val('" + dialog.FileName.Replace("\\", "\\\\").Replace("'", "\\'") + "');" +
                    @"$('#folderPath').change();";
                    this.WebBrowser.ExecuteScriptAsync(script);
                }
            });
        }

        protected override void OnClose()
        {
            this.WebBrowser?.Dispose();
            this.WebBrowser = null;

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

        private Uri GetSyncthingAddress()
        {
            // SyncthingManager will always request over HTTPS, whether Syncthing enforces this or not.
            // However in an attempt to avoid #201 we'll use HTTP if available, and if not Syncthing will redirect us.
            var uriBuilder = new UriBuilder(this.syncthingManager.Address.NormalizeZeroHost())
            {
                Scheme = "http"
            };
            return uriBuilder.Uri;
        }

        bool IRequestHandler.GetAuthCredentials(IWebBrowser browserControl, IBrowser browser, IFrame frame, bool isProxy, string host, int port, string realm, string scheme, IAuthCallback callback)
        {
            return false;
        }

        bool IRequestHandler.OnBeforeBrowse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, bool isRedirect)
        {
            return false;
        }

        CefReturnValue IRequestHandler.OnBeforeResourceLoad(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IRequestCallback callback)
        {
            var uri = new Uri(request.Url);
            // We can get http requests just after changing Syncthing's address: after we've navigated to about:blank but before navigating to
            // the new address (Which we do when Syncthing hits the 'running' State).
            // Therefore only open external browsers if Syncthing is actually running
            if (this.syncthingManager.State == SyncthingState.Running && (uri.Scheme == "http" || uri.Scheme == "https") && uri.Host != this.GetSyncthingAddress().Host)
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

        bool IRequestHandler.OnCertificateError(IWebBrowser browserControl, IBrowser browser, CefErrorCode errorCode, string requestUrl, ISslInfo sslInfo, IRequestCallback callback)
        {
            // We shouldn't hit this, since IgnoreCertificateErrors is true
            return true;
        }

        void IRequestHandler.OnPluginCrashed(IWebBrowser browserControl, IBrowser browser, string pluginPath)
        {
        }

        void IRequestHandler.OnRenderProcessTerminated(IWebBrowser browserControl, IBrowser browser, CefTerminationStatus status)
        {
        }

        bool IRequestHandler.OnOpenUrlFromTab(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, WindowOpenDisposition targetDisposition, bool userGesture)
        {
            return false;
        }

        bool IRequestHandler.OnQuotaRequest(IWebBrowser browserControl, IBrowser browser, string originUrl, long newSize, IRequestCallback callback)
        {
            callback.Continue(true);
            return true;
        }

        void IRequestHandler.OnResourceRedirect(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, ref string newUrl)
        {
        }

        bool IRequestHandler.OnProtocolExecution(IWebBrowser browserControl, IBrowser browser, string url)
        {
            return false;
        }

        void IRequestHandler.OnRenderViewReady(IWebBrowser browserControl, IBrowser browser)
        {
        }

        bool IRequestHandler.OnResourceResponse(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            return false;
        }

        IResponseFilter IRequestHandler.GetResourceResponseFilter(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response)
        {
            return null;
        }

        void IRequestHandler.OnResourceLoadComplete(IWebBrowser browserControl, IBrowser browser, IFrame frame, IRequest request, IResponse response, UrlRequestStatus status, long receivedContentLength)
        {
        }

        void ILifeSpanHandler.OnBeforeClose(IWebBrowser browserControl, IBrowser browser)
        {
        }

        bool ILifeSpanHandler.OnBeforePopup(IWebBrowser browserControl, IBrowser browser, IFrame frame, string targetUrl, string targetFrameName, WindowOpenDisposition targetDisposition, bool userGesture, IPopupFeatures popupFeatures, IWindowInfo windowInfo, IBrowserSettings browserSettings, ref bool noJavascriptAccess, out IWebBrowser newBrowser)
        {
            this.processStartProvider.StartDetached(targetUrl);
            newBrowser = null;
            return true;
        }

        void ILifeSpanHandler.OnAfterCreated(IWebBrowser browserControl, IBrowser browser)
        {
        }

        bool ILifeSpanHandler.DoClose(IWebBrowser browserControl, IBrowser browser)
        {
            return false;
        }

        void IContextMenuHandler.OnBeforeContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
        {
            // Clear the default menu, just leaving our custom one
            model.Clear();
        }

        bool IContextMenuHandler.OnContextMenuCommand(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
        {
            return false;
        }

        void IContextMenuHandler.OnContextMenuDismissed(IWebBrowser browserControl, IBrowser browser, IFrame frame)
        {
        }

        bool IContextMenuHandler.RunContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
        {
            return false;
        }

        public void Dispose()
        {
            this.syncthingManager.StateChanged -= this.SyncthingStateChanged;
            this.configurationProvider.ConfigurationChanged -= this.ConfigurationChanged;
        }

        private class JavascriptCallbackObject
        {
            private readonly Action<string> openFolderAction;
            private readonly Action browseFolderPathAction;

            public JavascriptCallbackObject(Action<string> openFolderAction, Action browseFolderPathAction)
            {
                this.openFolderAction = openFolderAction;
                this.browseFolderPathAction = browseFolderPathAction;
            }

            public void OpenFolder(string folderId)
            {
                this.openFolderAction(folderId);
            }

            public void BrowseFolderPath()
            {
                this.browseFolderPathAction();
            }
        }
    }
}
