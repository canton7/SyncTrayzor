using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interactivity;

namespace SyncTrayzor.Xaml
{
    public class CollapsingRowDefinitionBehaviour : Behavior<RowDefinition>
    {
        public GridLength Height
        {
            get { return (GridLength)GetValue(HeightProperty); }
            set { SetValue(HeightProperty, value); }
        }
        public static readonly DependencyProperty HeightProperty =
            DependencyProperty.Register("Height", typeof(GridLength), typeof(CollapsingRowDefinitionBehaviour), new PropertyMetadata(GridLength.Auto));

        public double MinHeight
        {
            get { return (double)GetValue(MinHeightProperty); }
            set { SetValue(MinHeightProperty, value); }
        }
        public static readonly DependencyProperty MinHeightProperty =
            DependencyProperty.Register("MinHeight", typeof(double), typeof(CollapsingRowDefinitionBehaviour), new PropertyMetadata(0.0));

        public Visibility RowVisibility
        {
            get { return (Visibility)GetValue(RowVisibilityProperty); }
            set { SetValue(RowVisibilityProperty, value); }
        }
        public static readonly DependencyProperty RowVisibilityProperty =
            DependencyProperty.Register("RowVisibility", typeof(Visibility), typeof(CollapsingRowDefinitionBehaviour), new PropertyMetadata(Visibility.Visible, (d, e) =>
            {
                ((CollapsingRowDefinitionBehaviour)d).Refresh();
            }));

        protected override void OnAttached()
        {
            this.Refresh();
        }

        protected override void OnDetaching()
        {
            BindingOperations.ClearBinding(this.AssociatedObject, RowDefinition.HeightProperty);
        }

        private void Refresh()
        {
            BindingOperations.ClearBinding(this.AssociatedObject, RowDefinition.HeightProperty);

            if (this.RowVisibility == Visibility.Collapsed)
            {
                this.AssociatedObject.Height = new GridLength(0);
                this.AssociatedObject.MinHeight = 0;
            }
            else
            {
                var heightBinding = new Binding("Height")
                {
                    Source = this,
                    Mode = BindingMode.TwoWay,
                };
                BindingOperations.SetBinding(this.AssociatedObject, RowDefinition.HeightProperty, heightBinding);

                this.AssociatedObject.MinHeight = this.MinHeight;
            }
        }
    }
}
