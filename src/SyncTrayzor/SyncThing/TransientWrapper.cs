using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.SyncThing
{
    public class TransientWrapperValueChangedEventArgs<T> : EventArgs
    {
        public T Value { get; private set; }

        public TransientWrapperValueChangedEventArgs(T value)
        {
            this.Value = value;
        }
    }

    public class TransientWrapper<T> where T : class
    {
        public event EventHandler<TransientWrapperValueChangedEventArgs<T>> ValueCreated;
        public event EventHandler<TransientWrapperValueChangedEventArgs<T>> ValueDestroyed;

        protected T _value;
        public virtual T Value
        {
            get { return this._value; }
            set
            {
                var oldValue = this._value;
                this._value = value;

                this.RaiseEvents(oldValue, value);
            }
        }

        public TransientWrapper()
        {
        }

        public TransientWrapper(T value)
        {
            this.Value = value;
        }

        protected void RaiseEvents(T oldValue, T newValue)
        {
            if (oldValue != null && newValue == null)
                this.OnValueDestroyed(oldValue);
            else if (oldValue == null && newValue != null)
                this.OnValueCreated(newValue);
        }

        private void OnValueCreated(T value)
        {
            var handler = this.ValueCreated;
            if (handler != null)
                handler(this, new TransientWrapperValueChangedEventArgs<T>(value));
        }

        private void OnValueDestroyed(T value)
        {
            var handler = this.ValueDestroyed;
            if (handler != null)
                handler(this, new TransientWrapperValueChangedEventArgs<T>(value));
        }
    }

    public class SynchronizedTransientWrapper<T> : TransientWrapper<T> where T : class
    {
        private readonly object _lockObject;
        public object LockObject
        {
            get { return this._lockObject; }
        }

        public override T Value
        {
            get
            {
                lock (this._lockObject)
                {
                    return base.Value;
                }
            }
            set
            {
                T oldValue;
                lock (this._lockObject)
                {
                    oldValue = this._value;
                    this._value = value;
                }

                this.RaiseEvents(oldValue, value);
            }
        }

        public T UnsynchronizedValue
        {
            get { return base.Value; }
            set { base.Value = value; }
        }

        public SynchronizedTransientWrapper()
        {
            this._lockObject = new object();
        }

        public SynchronizedTransientWrapper(object lockObject)
        {
            this._lockObject = lockObject;
        }

        public SynchronizedTransientWrapper(T value)
        {
            this._lockObject = new object();
            this.Value = value;
        }

        public SynchronizedTransientWrapper(T value, object lockObject)
        {
            this._lockObject = lockObject;
            this.Value = value;
        }
    }
}
