using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace SyncTrayzor.Localization
{
    /// <summary>
    /// Localization converter which takes a static key and is applied to a single value
    /// </summary>
    public class StaticKeySingleValueConverter : IValueConverter
    {
        /// <summary>
        ///  Key to use to look up a localized string
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        ///  Optional converter to apply to the value
        /// </summary>
        public IValueConverter Converter { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (this.Key == null)
                throw new InvalidOperationException("Key must not be null");

            var convertedValue = (this.Converter == null) ? value : this.Converter.Convert(value, targetType, parameter, culture);
            return Localizer.Translate(this.Key, convertedValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    ///  Localization converter which takes a static key and is applied to multiple values
    /// </summary>
    public class StaticKeyMultipleValuesConverter : IMultiValueConverter
    {
        /// <summary>
        ///  Key to use to look up a localized string
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        ///  Optional converter to apply to the values. May return a scalar or an array
        /// </summary>
        public IMultiValueConverter Converter { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (this.Key == null)
                throw new InvalidOperationException("Key must not be null");

            if (this.Converter == null)
                return Localizer.Translate(this.Key, values);

            var convertedValues = this.Converter.Convert(values, targetType, parameter, culture);
            return Localizer.Translate(this.Key, convertedValues as object[] ?? new object[] { convertedValues });
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    ///  Localization converter which is applied to a key binding
    /// </summary>
    public class BoundKeyNoValuesConverter : IValueConverter
    {
        /// <summary>
        ///  Optional converter to apply to the key binding first
        /// </summary>
        public IValueConverter Converter { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var stringValue = (this.Converter == null) ? value as string : this.Converter.Convert(value, targetType, parameter, culture) as string;
            return (stringValue != null) ? Localizer.Translate(stringValue) : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    ///  Localization converter which is applied to a MultiBinding of { Key Binding, Value Binding, [Value Binding [...]]
    /// </summary>
    public class BoundKeyWithValuesConverter : IMultiValueConverter
    {
        /// <summary>
        ///  Optional converter to apply to the value bindings (not the key binding)
        /// </summary>
        public IMultiValueConverter ValuesConverter { get; set; }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // The first item is the key. The rest are the values

            if (values.Length < 1)
                return null;

            var key = values[0] as string;
            if (key == null)
                return null;

            var parameters = values.Skip(1).ToArray();
            if (this.ValuesConverter == null)
                return Localizer.Translate(key, parameters);

            var convertedParameters = this.ValuesConverter.Convert(parameters, targetType, parameter, culture);
            return Localizer.Translate(key, convertedParameters as object[] ?? new object[] { convertedParameters });
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
