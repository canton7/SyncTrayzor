using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SyncTrayzor.Xaml
{
    public class RemoveMnemonicsConverter : IValueConverter
    {
        public static RemoveMnemonicsConverter Instance = new RemoveMnemonicsConverter();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var val = value as string;
            if (val == null)
                return null;

            return val.Replace("_", "__");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var val = value as string;
            if (val == null)
                return null;

            return val.Replace("__", "_");
        }
    }
}
