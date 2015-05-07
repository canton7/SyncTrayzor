using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
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
