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
            this.AssociatedObject.TextChanged += TextChanged;

            this.AssociatedObject.ScrollToEnd();
        }

        protected override void DetachHandlers()
        {
            this.AssociatedObject.TextChanged -= TextChanged;
        }

        private void TextChanged(object sender, TextChangedEventArgs e)
        {
            this.AssociatedObject.ScrollToEnd();
        }
    }
}
