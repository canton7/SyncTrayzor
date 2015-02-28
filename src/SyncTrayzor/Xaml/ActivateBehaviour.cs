using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interactivity;

namespace SyncTrayzor.Xaml
{
    // From http://stackoverflow.com/a/12254217/1086121
    public class ActivateBehaviour : Behavior<Window>
    {
        private bool isActivated;

        public bool Activated
        {
            get { return (bool)GetValue(ActivatedProperty); }
            set { SetValue(ActivatedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Activated.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ActivatedProperty =
            DependencyProperty.Register("Activated", typeof(bool), typeof(ActivateBehaviour),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) =>
            {
                var behavior = (ActivateBehaviour)d;
                if (!behavior.Activated || behavior.isActivated)
                    return;
                // The Activated property is set to true but the Activated event (tracked by the
                // isActivated field) hasn't been fired. Go ahead and activate the window.
                if (behavior.AssociatedObject.WindowState == WindowState.Minimized)
                    behavior.AssociatedObject.WindowState = WindowState.Normal;
                behavior.AssociatedObject.Activate();
            }));

        protected override void OnAttached()
        {
            AssociatedObject.Activated += OnActivated;
            AssociatedObject.Deactivated += OnDeactivated;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Activated -= OnActivated;
            AssociatedObject.Deactivated -= OnDeactivated;
        }

        private void OnActivated(object sender, EventArgs eventArgs)
        {
            this.isActivated = true;
            Activated = true;
        }

        private void OnDeactivated(object sender, EventArgs eventArgs)
        {
            this.isActivated = false;
            Activated = false;
        }
    }
}
