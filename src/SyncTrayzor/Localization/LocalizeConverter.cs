using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace SyncTrayzor.Localization
{
    public class LocalizeConverter : DependencyObject, IValueConverter, IMultiValueConverter
    {
        /// <summary>
        /// This singleton avoid the need to declare a resource before using the class. Instead use {x:Static ...}
        /// </summary>
        public static readonly LocalizeConverter Singleton = new LocalizeConverter();

        /// <summary>
        /// Can be set, in which case it is used as the key for the property given to Convert
        /// </summary>
        public string Key
        {
            get { return (string)GetValue(KeyProperty); }
            set { SetValue(KeyProperty, value); }
        }

        public static readonly DependencyProperty KeyProperty =
            DependencyProperty.Register("Key", typeof(string), typeof(LocalizeConverter), null);

        public IValueConverter Converter
        {
            get { return (IValueConverter)GetValue(ConverterProperty); }
            set { SetValue(ConverterProperty, value); }
        }

        public static readonly DependencyProperty ConverterProperty =
            DependencyProperty.Register("Converter", typeof(IValueConverter), typeof(LocalizeConverter), null);


        public string StringFormat
        {
            get { return (string)GetValue(StringFormatProperty); }
            set { SetValue(StringFormatProperty, value); }
        }

        public static readonly DependencyProperty StringFormatProperty =
            DependencyProperty.Register("StringFormat", typeof(string), typeof(LocalizeConverter), new PropertyMetadata(null));

        
        /// <summary>
        /// If Key is specified, Takes a resource key in Key, and binds to the argument. Else uses value as the key
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // If this.Key is set, we're the value. If not, we're the key
            string result;

            if (this.Converter != null)
                value = this.Converter.Convert(value, targetType, parameter, culture);

            if (this.Key != null)
                result = Localizer.Translate(this.Key, value);
            else if (value is string)
                result = Localizer.Translate((string)value);
            else
                result = null;

            if (this.StringFormat != null)
                result = String.Format(this.StringFormat, result);

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// As a MultiValueConverter, if this Key is set, then we're the values. If it isn't, then values[0] is the key
        /// </summary>
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string result;

            if (this.Key == null)
            {
                if (values.Length < 1)
                    result = null;
                else if (values[0] is string)
                    result = Localizer.Translate((string)values[0], values.Skip(1).ToArray());
                else
                    return null;
            }
            else
            {
                result = Localizer.Translate(this.Key, values);
            }

            return result;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
