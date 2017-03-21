using System;

namespace SyncTrayzor.Syncthing
{
    public class TransientWrapperValueChangedEventArgs<T> : EventArgs
    {
        public T Value { get; }

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
            get => this._value;
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
            this.ValueCreated?.Invoke(this, new TransientWrapperValueChangedEventArgs<T>(value));
        }

        private void OnValueDestroyed(T value)
        {
            this.ValueDestroyed?.Invoke(this, new TransientWrapperValueChangedEventArgs<T>(value));
        }
    }

    public class SynchronizedTransientWrapper<T> : TransientWrapper<T> where T : class
    {
        private readonly object _lockObject;
        public object LockObject => this._lockObject;

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
            get => base.Value;
            set => base.Value = value;
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

        public T GetAsserted()
        {
            lock (this._lockObject)
            {
                if (base.Value == null)
                    throw new InvalidOperationException("Synchronized value is null");

                return base.Value;
            }
        }
    }
}
