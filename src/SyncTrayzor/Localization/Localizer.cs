using SmartFormat;
using SmartFormat.Extensions;
using SyncTrayzor.Properties;
using System;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace SyncTrayzor.Localization
{
    public static class Localizer
    {
        private static readonly SmartFormatter formatter;
        private static readonly CultureInfo baseCulture = new CultureInfo("en-US", false);

        public static FlowDirection FlowDirection => Thread.CurrentThread.CurrentUICulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        
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
                new CustomPluralLocalizationFormatter("en"),
                new ChooseFormatter(),
                new DefaultFormatter()
            );
        }

        public static string Translate(string key, params object[] parameters)
        {
            var culture = Thread.CurrentThread.CurrentUICulture;

            var format = Resources.ResourceManager.GetString(key, culture);

            if (format == null)
                format = Resources.ResourceManager.GetString(key, baseCulture);

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
            return Resources.ResourceManager.GetString(key, baseCulture);
        }
    }
}
