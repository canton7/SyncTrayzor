using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace SyncTrayzor.Localization
{
    public class LocExtension : MarkupExtension
    {
        public string Key { get; set; }

        public Binding KeyBinding { get; set; }

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

            // Most of these conditions are redundent, according to the assertions above. However I'll still state them,
            // for clarity.

            // A static key, and no values
            if (this.Key != null && this.KeyBinding == null && this.ValueBinding == null && this.ValueBindings == null)
            {
                // Just returning a string!
                return Localizer.Translate(this.Key);
            }
            // A static key, and a single value
            if (this.Key != null && this.KeyBinding == null && this.ValueBinding != null && this.ValueBindings == null)
            {
                var converter = new StaticKeySingleValueConverter() { Key = this.Key, Converter = this.ValueBinding.Converter };
                this.ValueBinding.Converter = converter;
                return this.ValueBinding.ProvideValue(serviceProvider);
            }
            // A static key, and multiple values
            if (this.Key != null && this.KeyBinding == null && this.ValueBinding == null && this.ValueBindings != null)
            {
                var converter = new StaticKeyMultipleValuesConverter() { Key = this.Key, Converter = this.ValueBindings.Converter };
                this.ValueBindings.Converter = converter;
                return this.ValueBindings.ProvideValue(serviceProvider);
            }
            // A bound key, no values
            if (this.Key == null && this.KeyBinding != null && this.ValueBinding == null && this.ValueBindings == null)
            {
                var converter = new BoundKeyNoValuesConverter() { Converter = this.KeyBinding.Converter };
                this.KeyBinding.Converter = converter;
                return this.KeyBinding.ProvideValue(serviceProvider);
            }
            // A bound key, and one value
            if (this.Key == null && this.KeyBinding != null && this.ValueBinding != null && this.ValueBindings == null)
            {
                var converter = new BoundKeyWithValuesConverter();
                var multiBinding = new MultiBinding() { Converter = converter };
                multiBinding.Bindings.Add(this.KeyBinding);
                multiBinding.Bindings.Add(this.ValueBinding);
                return multiBinding.ProvideValue(serviceProvider);
            }
            // A bound key, and multiple values
            if (this.Key == null && this.KeyBinding != null && this.ValueBinding == null && this.ValueBindings != null)
            {
                var converter = new BoundKeyWithValuesConverter() { ValuesConverter = this.ValueBindings.Converter };
                this.ValueBindings.Bindings.Insert(0, this.KeyBinding);
                this.ValueBindings.Converter = converter;
                return this.ValueBindings.ProvideValue(serviceProvider);
            }

            throw new Exception("Should never get here");
        }
    }
}
