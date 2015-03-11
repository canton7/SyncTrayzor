using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SyncTrayzor.Localization
{
    public static class Loc
    {
        public static string GetPrefix(DependencyObject obj)
        {
            return (string)obj.GetValue(PrefixProperty);
        }

        public static void SetPrefix(DependencyObject obj, string value)
        {
            obj.SetValue(PrefixProperty, value);
        }

        public static readonly DependencyProperty PrefixProperty =
            DependencyProperty.RegisterAttached("Prefix", typeof(string), typeof(Loc), new PropertyMetadata(String.Empty));
    }
}
