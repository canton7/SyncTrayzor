using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncTrayzor.Utils
{
    public class SlimObservable<T> : IObservable<T>
    {
        private readonly List<IObserver<T>> observers = new List<IObserver<T>>();

        public IDisposable Subscribe(IObserver<T> observer)
        {
            this.observers.Add(observer);
            return new Registration() { Parent = this, Observer = observer };
        }

        public void Publish(T value)
        {
            foreach (var observer in this.observers)
            {
                observer.OnNext(value);
            }
        }

        private class Registration : IDisposable
        {
            public SlimObservable<T> Parent;
            public IObserver<T> Observer;

            public void Dispose()
            {
                this.Parent.observers.Remove(this.Observer);
            }
        }
    }

    public class SlimObserver<T> : IObserver<T>
    {
        private readonly Action<T> action;

        public SlimObserver(Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            this.action = action;
        }

        public void OnNext(T value)
        {
            this.action(value);
        }

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }
    }

    public static class SlimObservableExtensions
    {
        public static IDisposable Subscribe<T>(this IObservable<T> observable, Action<T> action)
        {
            var observer = new SlimObserver<T>(action);
            return observable.Subscribe(observer);
        }
    }
}
