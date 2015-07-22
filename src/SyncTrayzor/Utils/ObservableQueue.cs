using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace SyncTrayzor.Utils
{
    public class ObservableQueue<T> : IEnumerable<T>, INotifyCollectionChanged
    {
        private readonly Queue<T> queue = new Queue<T>();

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public int Count => this.queue.Count;

        public void Enqueue(T item)
        {
            this.queue.Enqueue(item);
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, this.queue.Count - 1));
        }

        public T Dequeue()
        {
            T item = this.queue.Dequeue();
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, 0));
            return item;
        }

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            this.CollectionChanged?.Invoke(this, e);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.queue.GetEnumerator();
        }
    }
}
