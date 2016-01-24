using SmartFormat;
using SmartFormat.Extensions;
using SyncTrayzor.Properties;
using System;
using System.Globalization;
using System.Threading;

namespace SyncTrayzor.Localization
{
    public static class Localizer
    {
        private static readonly SmartFormatter formatter;

        static Localizer()
        {
            formatter = new SmartFormatter();

            var listFormatter = new ListFormatter(formatter);

            formatter.AddExtensions(
                listFormatter,
                new DefaultSource(formatter)
            );

            formatter.AddExtensions(
                listFormatter,
                new PluralLocalizationFormatter("en"),
                new ChooseFormatter(),
                new DefaultFormatter()
            );
        }

        public static string Translate(string key, params object[] parameters)
        {
            var culture = Thread.CurrentThread.CurrentUICulture;

            var format = Resources.ResourceManager.GetString(key, culture);
            
            if (format == null)
                return "!" + key + (parameters.Length > 0 ? ":" + String.Join(",", parameters) : "") + "!";

            return formatter.Format(culture, format, parameters);
        }

        public static string F(string format, params object[] parameters)
        {
            return formatter.Format(Thread.CurrentThread.CurrentUICulture, format, parameters);
        }

        public static string OriginalTranslation(string key)
        {
            return Resources.ResourceManager.GetString(key, new CultureInfo("en-US", false));
        }
    }
}
