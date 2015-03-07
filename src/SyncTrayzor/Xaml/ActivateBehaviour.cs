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
    public class ActivateBehaviour : DetachingBehaviour<Window>
    {
        private bool altering;

        public bool Activated
        {
            get { return (bool)GetValue(ActivatedProperty); }
            set { SetValue(ActivatedProperty, value); }
        }

        public static readonly DependencyProperty ActivatedProperty =
            DependencyProperty.Register("Activated", typeof(bool), typeof(ActivateBehaviour),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, e) =>
            {
                var behaviour = (ActivateBehaviour)d;
                if (behaviour.altering || !(bool)e.NewValue)
                    return;

                if (behaviour.AssociatedObject.WindowState == WindowState.Minimized)
                    behaviour.AssociatedObject.WindowState = WindowState.Normal;

                behaviour.AssociatedObject.Activate();
            }));

        protected override void AttachHandlers()
        {
            AssociatedObject.Activated += OnActivated;
            AssociatedObject.Deactivated += OnDeactivated;
        }

        protected override void DetachHandlers()
        {
            AssociatedObject.Activated -= OnActivated;
            AssociatedObject.Deactivated -= OnDeactivated;
        }

        private void OnActivated(object sender, EventArgs eventArgs)
        {
            this.altering = true;
            Activated = true;
            this.altering = false;
        }

        private void OnDeactivated(object sender, EventArgs eventArgs)
        {
            Activated = false;
        }
    }
}
