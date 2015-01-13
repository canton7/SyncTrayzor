using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SyncTrayzor.Xaml
{
    public static class WebBrowserUtilities
    {
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

        
    }
}
