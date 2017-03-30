using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace SyncTrayzor.Xaml
{
    public class GridViewSortBy : DependencyObject
    {
        public static string GetSortByKey(DependencyObject obj)
        {
            return (string)obj.GetValue(SortByKeyProperty);
        }

        public static void SetSortByKey(DependencyObject obj, string value)
        {
            obj.SetValue(SortByKeyProperty, value);
        }

        public static readonly DependencyProperty SortByKeyProperty =
            DependencyProperty.RegisterAttached("SortByKey", typeof(string), typeof(GridViewSortBy), new PropertyMetadata(null));


        public static bool GetIsInitiallySorted(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsInitiallySortedProperty);
        }

        public static void SetIsInitiallySorted(DependencyObject obj, bool value)
        {
            obj.SetValue(IsInitiallySortedProperty, value);
        }

        public static readonly DependencyProperty IsInitiallySortedProperty =
            DependencyProperty.RegisterAttached("IsInitiallySorted", typeof(bool), typeof(GridViewSortBy), new PropertyMetadata(false));


    }

    public class GridViewSortAdorner : Adorner
    {
        // See http://www.wpf-tutorial.com/listview-control/listview-how-to-column-sorting/

        private static Geometry ascendingArrow = Geometry.Parse("M 0 0 L 3.5 4 L 7 0 Z");
        private static Geometry descendingArrow = Geometry.Parse("M 0 4 L 3.5 0 L 7 4 Z");

        public ListSortDirection Direction { get; private set; }

        public GridViewSortAdorner(UIElement element, ListSortDirection dir)          
            : base(element)
        {
            this.Direction = dir;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (AdornedElement.RenderSize.Width < 20)
                return;

            TranslateTransform transform = new TranslateTransform(AdornedElement.RenderSize.Width - 12, (AdornedElement.RenderSize.Height - 5) / 2);
            drawingContext.PushTransform(transform);

            var geometry = (this.Direction == ListSortDirection.Ascending) ? ascendingArrow : descendingArrow;

            drawingContext.DrawGeometry(Brushes.Black, null, geometry);

            drawingContext.Pop();
        }

    }

    public class GridViewSortByBehaviour : DetachingBehaviour<ListView>
    {
        private GridViewColumnHeader lastColumnHeader;
        private ListSortDirection lastDirection;
        private GridViewSortAdorner lastAdorner;

        protected override void AttachHandlers()
        {
            this.AssociatedObject.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(this.GridViewColumnHeaderClicked));
            this.AssociatedObject.Loaded += this.ListViewLoaded;
        }

        protected override void DetachHandlers()
        {
            this.AssociatedObject.RemoveHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(this.GridViewColumnHeaderClicked));
            this.AssociatedObject.Loaded -= this.ListViewLoaded;
        }

        private void GridViewColumnHeaderClicked(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            if (headerClicked == null)
                return;

            this.SortBy(headerClicked);
        }

        private void ListViewLoaded(object sender, RoutedEventArgs e)
        {
            this.PerformInitialSort();
        }

        private void PerformInitialSort()
        {
            var gridView = this.AssociatedObject.View as GridView;
            if (gridView == null)
                return;


            var initialSortColumn = gridView.Columns.FirstOrDefault(x => GridViewSortBy.GetIsInitiallySorted(x));
            if (initialSortColumn == null)
                return;

            if (initialSortColumn.Header is GridViewColumnHeader header)
                this.ApplyColumnSort(header, initialSortColumn, ListSortDirection.Ascending);
        }

        private void SortBy(GridViewColumnHeader header)
        {
            var direction = ListSortDirection.Ascending;
            if (header == this.lastColumnHeader)
                direction = (this.lastDirection == ListSortDirection.Ascending) ? ListSortDirection.Descending : ListSortDirection.Ascending;

            this.ApplyColumnSort(header, header.Column, direction);
        }

        private void ApplyColumnSort(GridViewColumnHeader header, GridViewColumn column, ListSortDirection direction)
        {
            var propertyName = GridViewSortBy.GetSortByKey(column);
            if (propertyName == null)
                return;

            // So AdornerLayer.GetAdornerLayer can apparently sometimes return null, even though we're calling it from
            // the Loaded event. Maybe it's because the Window hasn't yet fully loaded? Don't crash in this case
            // anyway: we won't show the little arrow, but that's not the end of the world.

            if (this.lastColumnHeader != null && this.lastAdorner != null)
                AdornerLayer.GetAdornerLayer(this.lastColumnHeader)?.Remove(this.lastAdorner);

            var adorner = new GridViewSortAdorner(header, direction);
            AdornerLayer.GetAdornerLayer(header)?.Add(adorner);

            var collectionView = CollectionViewSource.GetDefaultView(this.AssociatedObject.ItemsSource);

            collectionView.SortDescriptions.Clear();
            collectionView.SortDescriptions.Add(new SortDescription(propertyName, direction));
            collectionView.Refresh();

            this.lastColumnHeader = header;
            this.lastDirection = direction;
            this.lastAdorner = adorner;
        }
    }
}
