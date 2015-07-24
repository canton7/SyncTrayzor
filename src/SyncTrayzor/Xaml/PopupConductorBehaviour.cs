using Stylet;
using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace SyncTrayzor.Xaml
{
    public class PopupConductorBehaviour : DetachingBehaviour<Popup>
    {
        public object DataContext
        {
            get { return (object)GetValue(DataContextProperty); }
            set { SetValue(DataContextProperty, value); }
        }

        public static readonly DependencyProperty DataContextProperty =
            DependencyProperty.Register("DataContext", typeof(object), typeof(PopupConductorBehaviour), new PropertyMetadata(null));

        protected override void AttachHandlers()
        {
            this.AssociatedObject.Opened += this.Opened;
            this.AssociatedObject.Closed += this.Closed;
        }

        protected override void DetachHandlers()
        {
            this.AssociatedObject.Opened -= this.Opened;
            this.AssociatedObject.Closed -= this.Closed;
        }

        private void Opened(object sender, EventArgs e)
        {
            var screenState = this.DataContext as IScreenState;
            if (screenState != null)
                screenState.Activate();
        }

        private void Closed(object sender, EventArgs e)
        {
            var screenState = this.DataContext as IScreenState;
            if (screenState != null)
                screenState.Close();
        }
    }
}
