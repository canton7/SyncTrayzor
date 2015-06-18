using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.Utils
{
    public class SynchronizedEventDispatcher
    {
        private readonly object sender;
        private readonly SynchronizationContext synchronizationContext;

        public SynchronizedEventDispatcher(object sender)
            : this(sender, SynchronizationContext.Current)
        {
            if (SynchronizationContext.Current == null)
                throw new ArgumentNullException("Implicit SynchronizationContext.Current cannot be null");
        }

        public SynchronizedEventDispatcher(object sender, SynchronizationContext synchronizationContext)
        {
            this.sender = sender;
            this.synchronizationContext = synchronizationContext;
        }

        public void Raise(EventHandler eventHandler)
        {
            if (eventHandler != null)
                this.Post(_ => eventHandler(this.sender, EventArgs.Empty), null);
        }

        public void Raise<T>(EventHandler<T> eventHandler, T eventArgs)
        {
            if (eventHandler != null)
                this.Post(_ => eventHandler(this.sender, eventArgs), null);
        }

        public void Raise<T>(EventHandler<T> eventHandler, Func<T> eventArgs)
        {
            if (eventHandler != null)
                this.Post(_ => eventHandler(this.sender, eventArgs()), null);
        }

        private void Post(SendOrPostCallback callback, object state)
        {
            if (this.synchronizationContext != null)
                this.synchronizationContext.Post(callback, state);
            else
                callback(state);
        }
    }
}
