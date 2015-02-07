using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SyncTrayzor.Xaml
{
    public class WebBrowserUtilities
    {
        private static readonly Guid sid_webBrowserApp = new Guid("0002DF05-0000-0000-C000-000000000046");

        public static string GetLocation(DependencyObject obj)
        {
            return (string)obj.GetValue(LocationProperty);
        }

        public static void SetLocation(DependencyObject obj, string value)
        {
            obj.SetValue(LocationProperty, value);
        }

        // Using a DependencyProperty as the backing store for Location.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LocationProperty =
            DependencyProperty.RegisterAttached("Location", typeof(string), typeof(WebBrowserUtilities), new PropertyMetadata(null, (d, e) =>
            {
                var webBrowser = (WebBrowser)d;
                if (e.NewValue != null)
                    webBrowser.Navigate((string)e.NewValue);
                else
                    webBrowser.Navigate("about:blank");
            }));


        // http://blogs.microsoft.co.il/shair/2011/09/05/wpf-webbrowser-how-to-disable-sound/
        private const int FeatureDisableNavigationSounds = 21;
        private const int SetFeatureOnProcess = 0x00000002;

        [DllImport("urlmon.dll")]
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.Error)]
        private static extern int CoInternetSetFeatureEnabled(int featureEntry, [MarshalAs(UnmanagedType.U4)] int dwFlags, bool fEnable);

        public static bool GetDisableNavigationSounds(DependencyObject obj)
        {
            return (bool)obj.GetValue(DisableNavigationSoundsProperty);
        }

        public static void SetDisableNavigationSounds(DependencyObject obj, bool value)
        {
            obj.SetValue(DisableNavigationSoundsProperty, value);
        }

        // Using a DependencyProperty as the backing store for DisableNavigationSounds.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DisableNavigationSoundsProperty =
            DependencyProperty.RegisterAttached("DisableNavigationSounds", typeof(bool), typeof(WebBrowserUtilities), new PropertyMetadata(false, (d, e) =>

        {
            if (!(e.NewValue is bool))
                return;

            if ((bool)e.NewValue)
                CoInternetSetFeatureEnabled(FeatureDisableNavigationSounds, SetFeatureOnProcess, true);
            else
                CoInternetSetFeatureEnabled(FeatureDisableNavigationSounds, SetFeatureOnProcess, false);
        }));


        public static bool GetPreventOpenExternalWindow(DependencyObject obj)
        {
            return (bool)obj.GetValue(PreventOpenExternalWindowProperty);
        }

        public static void SetPreventOpenExternalWindow(DependencyObject obj, bool value)
        {
            obj.SetValue(PreventOpenExternalWindowProperty, value);
        }

        // Using a DependencyProperty as the backing store for PreventOpenExternalWindow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PreventOpenExternalWindowProperty =
            DependencyProperty.RegisterAttached("PreventOpenExternalWindow", typeof(bool), typeof(WebBrowserUtilities), new PropertyMetadata(false, (d, e) =>
            {
                var webBrowser = d as WebBrowser;
                if (webBrowser == null || !(e.NewValue is bool))
                    return;

                if ((bool)e.NewValue)
                    webBrowser.LoadCompleted += loadCompleted;
                else
                    webBrowser.LoadCompleted -= loadCompleted;
            }));

        // http://social.technet.microsoft.com/wiki/contents/articles/22943.preventing-external-links-from-opening-in-new-window-in-wpf-web-browser.aspx

        private static void loadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            var webBrowser = sender as WebBrowser;
            if (webBrowser == null || webBrowser.Document == null)
                return;

            var serviceProvider = (IServiceProvider)webBrowser.Document;
            var serviceGuid = sid_webBrowserApp;
            var iid = typeof(SHDocVw.IWebBrowser2).GUID;
            var webBrowserService = (SHDocVw.IWebBrowser2)serviceProvider.QueryService(ref serviceGuid, ref iid);
            var wbEvents = (SHDocVw.DWebBrowserEvents_Event)webBrowserService;
            wbEvents.NewWindow += (string url, int flags, string targetFrameName, ref object postData, string headers, ref bool processed) =>
            {
                if (processed)
                    return;

                processed = true;
                webBrowser.RaiseEvent(new ExternalWindowOpenedEventArgs(WebBrowserUtilities.ExternalWindowOpenedEvent, url));
            };
        }

        public static readonly RoutedEvent ExternalWindowOpenedEvent = EventManager.RegisterRoutedEvent("ExternalWindowOpened", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(WebBrowserUtilities));
        public static void AddExternalWindowOpenedHandler(DependencyObject d, RoutedEventHandler handler)
        {
            UIElement uie = d as UIElement;
            if (uie != null)
                uie.AddHandler(WebBrowserUtilities.ExternalWindowOpenedEvent, handler);
        }
        public static void RemoveExternalWindowOpenedHandler(DependencyObject d, RoutedEventHandler handler)
        {
            UIElement uie = d as UIElement;
            if (uie != null)
                uie.RemoveHandler(WebBrowserUtilities.ExternalWindowOpenedEvent, handler);
        }

        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("6d5140c1-7436-11ce-8034-00aa006009fa")]
        internal interface IServiceProvider
        {
            [return: MarshalAs(UnmanagedType.IUnknown)]
            object QueryService(ref Guid guidService, ref Guid riid);
        }
        
    }

    public class ExternalWindowOpenedEventArgs : RoutedEventArgs
    {
        public string Url { get; private set; }

        public ExternalWindowOpenedEventArgs(RoutedEvent routedEvent, string url)
            : base(routedEvent)
        {
            this.Url = url;
        }
    }
}
