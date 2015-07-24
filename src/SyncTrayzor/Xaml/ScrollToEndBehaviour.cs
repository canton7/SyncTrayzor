using System;
using System.Windows.Controls;

namespace SyncTrayzor.Xaml
{
    public class ScrollToEndBehaviour : DetachingBehaviour<TextBox>
    {
        protected override void AttachHandlers()
        {
            this.AssociatedObject.TextChanged += this.SomethingChanged;
            this.AssociatedObject.SizeChanged += this.SomethingChanged;

            this.AssociatedObject.ScrollToEnd();
        }

        protected override void DetachHandlers()
        {
            this.AssociatedObject.TextChanged -= this.SomethingChanged;
            this.AssociatedObject.SizeChanged -= this.SomethingChanged;
        }

        private void SomethingChanged(object sender, EventArgs e)
        {
            this.AssociatedObject.ScrollToEnd();
        }
    }
}
