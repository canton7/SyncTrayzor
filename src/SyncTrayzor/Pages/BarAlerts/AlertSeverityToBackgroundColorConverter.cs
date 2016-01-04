using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace SyncTrayzor.Pages.BarAlerts
{
    public class AlertSeverityToBackgroundColorConverter : IValueConverter
    {
        public static readonly AlertSeverityToBackgroundColorConverter Instance = new AlertSeverityToBackgroundColorConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is AlertSeverity))
                return null;

            var severity = (AlertSeverity)value;

            switch (severity)
            {
                case AlertSeverity.Warning:
                    return new SolidColorBrush(Color.FromArgb(125, 255, 255, 0));

                default:
                    Debug.Assert(false);
                    return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
