using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interactivity;

namespace SyncTrayzor.Xaml
{
    // Adapted from http://dotnetbyexample.blogspot.co.uk/2011/04/safe-event-detachment-pattern-for.html
    public abstract class DetachingBehaviour<T> : Behavior<T> where T : FrameworkElement
    {
        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.Initialized += this.AssociatedObjectInitialized;
            this.AssociatedObject.Unloaded += this.AssociatedObjectUnloaded;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.Cleanup();
        }

        private bool isCleanedUp;
        private void Cleanup()
        {
            if (!this.isCleanedUp)
            {
                this.isCleanedUp = true;

                this.AssociatedObject.Initialized -= AssociatedObjectInitialized;
                this.AssociatedObject.Unloaded -= AssociatedObjectUnloaded;
                BindingOperations.ClearAllBindings(this); // This was a surprise...
                this.DetachHandlers();
            }
        }

        private void AssociatedObjectInitialized(object sender, EventArgs e)
        {
            this.AttachHandlers();
        }

        private void AssociatedObjectUnloaded(object sender, RoutedEventArgs e)
        {
            this.Cleanup();
        }

        protected virtual void AttachHandlers() { }
        protected virtual void DetachHandlers() { }
    }
}
