using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace SyncTrayzor.Localization
{
    public class LocExtension : MarkupExtension
    {
        public string Key { get; set; }

        public Binding KeyBinding { get; set; }

        public string StringFormat { get; set; }

        public Binding ValueBinding { get; set; }

        public MultiBinding ValueBindings { get; set; }

        public LocExtension()
        {
        }

        public LocExtension(string key)
        {
            this.Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (this.Key == null && this.KeyBinding == null)
                throw new ArgumentException("Either Key or KeyBinding must be set");
            if (this.Key != null && this.KeyBinding != null)
                throw new ArgumentException("Either Key or KeyBinding must be set, but not both");
            if (this.ValueBinding != null && this.ValueBindings != null)
                throw new ArgumentException("ValueBinding and ValueBindings may not be set at the same time");

            // If we've got no bindings, return a string. If we've got 1 binding, return it. If we've got 2 bindings,
            // return a new MultiBinding.
            // Unfortunately there's no nice way to generalise this...

            if (this.KeyBinding == null && this.ValueBinding == null && this.ValueBindings == null)
            {
                // Just returning a string!
                return String.Format(this.StringFormat ?? "{0}", Localizer.Translate(this.Key));
            }

            if (this.ValueBinding != null || this.ValueBindings != null)
            {
                var converter = new LocaliseConverter();
                if (this.Key != null)
                    converter.Key = this.Key;
                else
                    return null;

                if (this.ValueBinding != null)
                {
                    converter.Converter = this.ValueBinding.Converter;
                    this.ValueBinding.Converter = converter;
                    return this.ValueBinding.ProvideValue(serviceProvider);
                }
                else
                {
                    // TODO: Chain converters here somehow?
                    this.ValueBindings.Converter = converter;
                    return this.ValueBindings.ProvideValue(serviceProvider);
                }
            }

            if (this.KeyBinding != null)
            {
                var converter = new LocaliseConverter();
                converter.Converter = this.KeyBinding.Converter;
                this.KeyBinding.Converter = converter;
                return this.KeyBinding.ProvideValue(serviceProvider);
            }

            // We're returning a string, no binding

            string result = null;

            if (this.Key != null)
            {
                result = Localizer.Translate(this.Key);
            }
            else if (this.KeyBinding != null)
            {
                var key = this.KeyBinding.ProvideValue(serviceProvider) as string;
                if (key == null)
                    return null;
                result = Localizer.Translate(key);
            }

            if (this.StringFormat != null)
                result = String.Format(this.StringFormat, result);

            return result;
        }
    }
}
