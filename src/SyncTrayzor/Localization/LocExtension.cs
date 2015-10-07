using System;
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

            var converter = new LocalizeConverter();
            converter.StringFormat = this.StringFormat;

            // Single binding case
            if (this.KeyBinding != null && this.ValueBinding == null && this.ValueBindings == null)
            {
                // Don't set the key, so it'll assume the binding is the key
                converter.Converter = this.KeyBinding.Converter;
                this.KeyBinding.Converter = converter;
                return this.KeyBinding.ProvideValue(serviceProvider);
            }
            if (this.KeyBinding == null && this.ValueBinding != null && this.ValueBindings == null)
            {
                // Set the key, it'll interpret the binding as the value
                converter.Key = this.Key;
                converter.Converter = this.ValueBinding.Converter;
                this.ValueBinding.Converter = converter;
                return this.ValueBinding.ProvideValue(serviceProvider);
            }
            if (this.KeyBinding == null && this.ValueBinding == null && this.ValueBindings != null)
            {
                converter.Key = this.Key;
                // No converter allowed here
                this.ValueBindings.Converter = converter;
                return this.ValueBindings.ProvideValue(serviceProvider);
            }

            MultiBinding multiBinding;

            // OK, multibinding cases
            // If this.ValueBindings is set, we'll hijack that
            // Otherwise, we'll create our own
            if (this.ValueBindings != null)
            {
                // Not setting converter.Converter - no support yet
                multiBinding = this.ValueBindings;
            }
            else // this.ValueBinding != null, according to preconditions
            {
                multiBinding = new MultiBinding();
                multiBinding.Bindings.Add(this.ValueBinding);
            }

            multiBinding.Converter = converter;
            if (this.Key != null) // Can't hit this case if ValueBinding != null
                converter.Key = this.Key;
            else
                multiBinding.Bindings.Insert(0, this.KeyBinding);

            return multiBinding.ProvideValue(serviceProvider);
        }
    }
}
