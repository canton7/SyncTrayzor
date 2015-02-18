using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SyncTrayzor.Xaml
{
    public static class ScrollViewerUtilities
    {


        public static NotifyCollectionChangedEventHandler GetScrollToEndSourceHandler(DependencyObject obj)
        {
            return (NotifyCollectionChangedEventHandler)obj.GetValue(ScrollToEndSourceHandlerProperty);
        }

        public static void SetScrollToEndSourceHandler(DependencyObject obj, NotifyCollectionChangedEventHandler value)
        {
            obj.SetValue(ScrollToEndSourceHandlerProperty, value);
        }

        // Using a DependencyProperty as the backing store for ScrollToEndSourceHandler.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScrollToEndSourceHandlerProperty =
            DependencyProperty.RegisterAttached("ScrollToEndSourceHandler", typeof(NotifyCollectionChangedEventHandler), typeof(ScrollViewerUtilities), new PropertyMetadata(null));

        

        public static INotifyCollectionChanged GetScrollToEndSource(DependencyObject obj)
        {
            return (INotifyCollectionChanged)obj.GetValue(ScrollToEndSourceProperty);
        }

        public static void SetScrollToEndSource(DependencyObject obj, INotifyCollectionChanged value)
        {
            obj.SetValue(ScrollToEndSourceProperty, value);
        }

        // Using a DependencyProperty as the backing store for ScrollToEndSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ScrollToEndSourceProperty =
            DependencyProperty.RegisterAttached("ScrollToEndSource", typeof(INotifyCollectionChanged), typeof(ScrollViewerUtilities), new PropertyMetadata(null, (d, e) =>
            {
                var scrollViewer = d as ScrollViewer;
                if (scrollViewer == null)
                    return;

                var newValue = e.NewValue as INotifyCollectionChanged;
                var oldValue = e.OldValue as INotifyCollectionChanged;

                if (oldValue != null)
                {
                    var oldHandler = GetScrollToEndSourceHandler(scrollViewer);
                    if (oldHandler != null)
                        oldValue.CollectionChanged -= oldHandler;
                }

                if (newValue != null)
                {
                    NotifyCollectionChangedEventHandler handler = (no, ne) =>
                    {
                        if (ne.Action == NotifyCollectionChangedAction.Add || ne.Action == NotifyCollectionChangedAction.Reset)
                            scrollViewer.ScrollToEnd();
                    };

                    SetScrollToEndSourceHandler(scrollViewer, handler);
                    newValue.CollectionChanged += handler;
                }

                scrollViewer.ScrollToEnd();
            }));
    }
}
