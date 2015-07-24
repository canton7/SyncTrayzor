using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Data;

namespace SyncTrayzor.Xaml
{
    public class LogMessageListToStringConverter : IValueConverter
    {
        private readonly StringBuilder sb = new StringBuilder();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var list = value as IEnumerable<string>;
            if (list == null)
                return null;

            foreach (var elem in list)
            {
                this.sb.AppendLine(elem);
            }

            var str = this.sb.ToString();
            this.sb.Clear();
            return str;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
