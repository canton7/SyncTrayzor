using Stylet;
using SyncTrayzor.Services.Config;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SyncTrayzor.Pages.Settings
{
    public abstract class SettingItem : ValidatingModelBase
    {
        public bool RequiresSyncthingRestart { get; set; }
        public bool RequiresSyncTrayzorRestart { get; set; }
        public abstract bool HasChanged { get; }
        public abstract void LoadValue(Configuration configuration);
        public abstract void SaveValue(Configuration configuration);
    }

    public class SettingItem<T> : SettingItem
    {
        private readonly Func<Configuration, T> getter;
        private readonly Action<Configuration, T> setter;
        private readonly Func<T, T, bool> comparer;

        public T OriginalValue { get; private set; }
        public T Value { get; set; }

        public override bool HasChanged => !this.comparer(this.OriginalValue, this.Value);

        public SettingItem(Expression<Func<Configuration, T>> accessExpression, IModelValidator validator = null, Func<T, T, bool> comparer = null)
        {
            var propertyName = accessExpression.NameForProperty();
            var propertyInfo = typeof(Configuration).GetProperty(propertyName);
            this.getter = c => (T)propertyInfo.GetValue(c);
            this.setter = (c, v) => propertyInfo.SetValue(c, v);
            this.comparer = comparer ?? new Func<T, T, bool>((x, y) => EqualityComparer<T>.Default.Equals(x, y));
            this.Validator = validator;
        }

        public SettingItem(Func<Configuration, T> getter, Action<Configuration, T> setter, IModelValidator validator = null, Func<T, T, bool> comparer = null)
        {
            this.getter = getter;
            this.setter = setter;
            this.comparer = comparer ?? new Func<T, T, bool>((x, y) => EqualityComparer<T>.Default.Equals(x, y));
            this.Validator = validator;
        }

        public override void LoadValue(Configuration configuration)
        {
            T value = this.getter(configuration);
            this.OriginalValue = value;
            this.Value = value;
        }

        public override void SaveValue(Configuration configuration)
        {
            this.setter(configuration, this.Value);
        }
    }
}
