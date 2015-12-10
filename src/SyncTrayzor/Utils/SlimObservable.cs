using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.Utils
{
    public class SlimObservable<T> : IObservable<T>
    {
        private readonly object observersLockObject = new object();
        private readonly HashSet<IObserver<T>> observers = new HashSet<IObserver<T>>();
        private readonly SynchronizationContext synchronizationContext;
        private bool complete;

        public SlimObservable()
        {
            this.synchronizationContext = SynchronizationContext.Current;
        }

        public IDisposable Subscribe(IObserver<T> observer)
        {
            lock (this.observersLockObject)
            {
                this.observers.Add(observer);
            }
            return new Registration() { Parent = this, Observer = observer };
        }

        private void Execute(Action<IObserver<T>> action)
        {
            if (this.synchronizationContext == null)
            {
                this.Perform(action);
            }
            else
            {
                this.synchronizationContext.Post(this.Perform, action);
            }
        }

        private void Perform(object state)
        {
            var action = (Action<IObserver<T>>)state;
            IObserver<T>[] observers;
            lock (this.observersLockObject)
            {
                observers = this.observers.ToArray();
            }

            foreach (var observer in observers)
            {
                action(observer);
            }
        }

        public void Next(T value)
        {
            this.Execute(o => o.OnNext(value));
        }

        public void Complete()
        {
            if (this.complete)
                return;
            this.complete = true;

            this.Execute(o => o.OnCompleted());
        }

        public void Error(Exception error)
        {
            if (this.complete)
                return;
            this.complete = true;

            this.Execute(o => o.OnError(error));
        }

        private class Registration : IDisposable
        {
            public SlimObservable<T> Parent;
            public IObserver<T> Observer;

            public void Dispose()
            {
                lock (this.Parent.observersLockObject)
                {
                    this.Parent.observers.Remove(this.Observer);
                }
            }
        }
    }

    public class SlimObserver<T> : IObserver<T>
    {
        private readonly Action<T> onNext;
        private readonly Action onCompleted;
        private readonly Action<Exception> onError;

        public SlimObserver(Action<T> onNext, Action onCompleted = null, Action<Exception> onError = null)
        {
            this.onNext = onNext;
            this.onCompleted = onCompleted;
            this.onError = onError;
        }

        public void OnNext(T value)
        {
            this.onNext?.Invoke(value);
        }

        public void OnCompleted()
        {
            this.onCompleted?.Invoke();
        }

        public void OnError(Exception error)
        {
            this.onError?.Invoke(error);
        }
    }

    public static class SlimObservableExtensions
    {
        public static IDisposable Subscribe<T>(this IObservable<T> observable, Action<T> onNext = null, Action onComplete = null, Action<Exception> onError = null)
        {
            var observer = new SlimObserver<T>(onNext, onComplete, onError);
            return observable.Subscribe(observer);
        }

        public static async Task SubscribeAsync<T>(this IObservable<T> observable, Action<T> onNext)
        {
            var tcs = new TaskCompletionSource<object>();

            var observer = new SlimObserver<T>(onNext, () => tcs.TrySetResult(null), e => tcs.TrySetException(e));
            using (observable.Subscribe(observer))
            {
                await tcs.Task;
            }
        }
    }
}
