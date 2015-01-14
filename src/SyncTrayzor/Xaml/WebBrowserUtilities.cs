using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SyncTrayzor.Xaml
{
    public static class WebBrowserUtilities
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
            }));


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
        
    }
}
