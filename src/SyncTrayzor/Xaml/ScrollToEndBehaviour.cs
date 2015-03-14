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
    public class ScrollToEndBehaviour : DetachingBehaviour<ScrollViewer>
    {
        public INotifyCollectionChanged Source
        {
            get { return (INotifyCollectionChanged)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(INotifyCollectionChanged), typeof(ScrollToEndBehaviour), new PropertyMetadata(null, (d, e) =>
            {
                ((ScrollToEndBehaviour)d).InccSubject(e.NewValue as INotifyCollectionChanged, e.OldValue as INotifyCollectionChanged);
            }));

        private void InccSubject(INotifyCollectionChanged newValue, INotifyCollectionChanged oldValue)
        {
            if (oldValue != null)
                oldValue.CollectionChanged -= this.OnCollectionChanged;

            if (newValue != null)
                newValue.CollectionChanged += this.OnCollectionChanged;
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Reset)
                this.AssociatedObject.ScrollToEnd();
        }
    }
}
