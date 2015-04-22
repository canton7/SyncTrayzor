using CefSharp.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Utils
{
    public static class ChromiumWebBrowserExtensions
    {
        private static MethodInfo setZoomLevelMethod = typeof(ChromiumWebBrowser).GetMethod("OnZoomLevelChanged", BindingFlags.Instance | BindingFlags.NonPublic, Type.DefaultBinder, new[] { typeof(double), typeof(double) }, null);

        public static void SetZoomLevel(this ChromiumWebBrowser browser, double zoomLevel)
        {
            // Yuck yuck yuck. This is fixed in CefSharp 39, but that breaks other things (I'm not entirely sure what, but things like
            // the device ID become broken).
            setZoomLevelMethod.Invoke(browser, new object[] { 0.0, zoomLevel });
        }
    }
}
