using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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
    }

    public class GridViewSortByBehaviour : DetachingBehaviour<ListView>
    {
        private string lastPropertyName;
        private ListSortDirection lastDirection;

        protected override void AttachHandlers()
        {
            this.AssociatedObject.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(this.GridViewColumnHeaderClicked));
        }

        protected override void DetachHandlers()
        {
            this.AssociatedObject.RemoveHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(this.GridViewColumnHeaderClicked));
        }

        private void GridViewColumnHeaderClicked(object sender, RoutedEventArgs e)
        {
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
            if (headerClicked == null)
                return;

            var sortBy = GridViewSortBy.GetSortByKey(headerClicked.Column);
            if (sortBy != null)
                this.SortBy(sortBy);
        }

        private void SortBy(string propertyName)
        {
            var collectionView = CollectionViewSource.GetDefaultView(this.AssociatedObject.ItemsSource);

            if (this.lastPropertyName == null)
            {
                // No previous? Populate with the initial from the existing sort descriptions
                var sortDescription = collectionView.SortDescriptions.FirstOrDefault();
                if (sortDescription != null)
                {
                    this.lastPropertyName = sortDescription.PropertyName;
                    this.lastDirection = sortDescription.Direction;
                }
            }

            var direction = ListSortDirection.Ascending;
            if (propertyName == this.lastPropertyName)
                direction = (this.lastDirection == ListSortDirection.Ascending) ? ListSortDirection.Descending : ListSortDirection.Ascending;

            this.lastPropertyName = propertyName;
            this.lastDirection = direction;

            collectionView.SortDescriptions.Clear();
            collectionView.SortDescriptions.Add(new SortDescription(propertyName, direction));
            collectionView.Refresh();
        }
    }
}
