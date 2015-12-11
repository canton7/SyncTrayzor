using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SyncTrayzor.Xaml
{
    public class GridViewSortByBehaviour : DetachingBehaviour<ListView>
    {
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

            this.SortBy("FileName");
        }

        private void SortBy(string propertyName)
        {
            var collectionView = CollectionViewSource.GetDefaultView(this.AssociatedObject.ItemsSource);
            collectionView.SortDescriptions.Clear();
            collectionView.SortDescriptions.Add(new SortDescription(propertyName, ListSortDirection.Descending));
            collectionView.Refresh();
        }
    }
}
