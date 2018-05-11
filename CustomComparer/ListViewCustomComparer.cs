using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using Hani.Utilities;

namespace CustomComparer
{
    internal class SortAdorner : Adorner
    {
        private readonly static Geometry _AscGeometry = Geometry.Parse("M 0,0 L 10,0 L 5,5 Z");
        private readonly static Geometry _DescGeometry = Geometry.Parse("M 0,5 L 10,5 L 5,0 Z");

        public ListSortDirection Direction { get; private set; }

        public SortAdorner(UIElement element, ListSortDirection dir)
            : base(element)
        {
            Direction = dir;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (drawingContext == null) return;

            base.OnRender(drawingContext);
            if (AdornedElement.RenderSize.Width < 20) return;

            drawingContext.PushTransform(
                new TranslateTransform(
                  AdornedElement.RenderSize.Width - 15,
                  (AdornedElement.RenderSize.Height - 5) / 2));

            drawingContext.DrawGeometry(SolidColors.DeepSkyBlue, null,
                Direction == ListSortDirection.Ascending ?
                  _AscGeometry : _DescGeometry);

            drawingContext.Pop();
        }
    }

    internal interface IListViewCustomComparer : IComparer
    {
        string SortBy { get; set; }
        ListSortDirection SortDirection { get; set; }
        int DirectionDig { get; set; }
    }

    internal abstract class ListViewCustomComparer<T> : IListViewCustomComparer where T : class
    {
        private string sortBy = string.Empty;
        public string SortBy
        {
            get { return sortBy; }
            set { sortBy = value; }
        }

        private ListSortDirection direction = ListSortDirection.Ascending;
        public ListSortDirection SortDirection
        {
            get { return direction; }
            set { direction = value; }
        }

        public int DirectionDig { get; set; }

        public int Compare(object x, object y)
        {
            return Compare(x as T, y as T);
        }

        internal abstract int Compare(T x, T y);
    }

    internal class ListViewSortItem
    {
        public ListViewSortItem(IListViewCustomComparer comparer, GridViewColumnHeader lastColumnHeaderClicked, ListSortDirection lastSortDirection)
        {
            Comparer = comparer;
            LastColumnHeaderClicked = lastColumnHeaderClicked;
            LastSortDirection = lastSortDirection;
        }

        public IListViewCustomComparer Comparer { get; private set; }

        public GridViewColumnHeader LastColumnHeaderClicked { get; set; }

        public ListSortDirection LastSortDirection { get; set; }

        public SortAdorner Adorner;
    }

    public static class ListViewSorter
    {
        private static Dictionary<string, ListViewSortItem> _listViewDefinitions = new Dictionary<string, ListViewSortItem>();

        public static readonly DependencyProperty CustomListViewSorterProperty = DependencyProperty.RegisterAttached(
            "CustomListViewSorter",
            typeof(string),
            typeof(ListViewSorter),
            new FrameworkPropertyMetadata("", new PropertyChangedCallback(OnRegisterSortableGrid)));

        public static string GetCustomListViewSorter(DependencyObject obj)
        {
            if (obj == null) return string.Empty;
            return (string)obj.GetValue(CustomListViewSorterProperty);
        }

        public static void SetCustomListViewSorter(DependencyObject obj, string value)
        {
            if (obj == null || value == null) return;
            obj.SetValue(CustomListViewSorterProperty, value);
        }

        public static void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            ListView view = sender as ListView;
            if (view == null) return;

            ListViewSortItem listViewSortItem = _listViewDefinitions[view.Name];
            if (listViewSortItem == null) return;

            GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
            if (headerClicked == null) return;

            ListCollectionView collectionView = CollectionViewSource.GetDefaultView(view.ItemsSource) as ListCollectionView;
            if (collectionView == null) return;

            ListSortDirection sortDirection = GetSortingDirection(headerClicked, listViewSortItem);

            string header = headerClicked.Tag as string;
            if (string.IsNullOrEmpty(header)) return;

            if (listViewSortItem.Comparer != null)
            {
                view.Cursor = System.Windows.Input.Cursors.Wait;
                listViewSortItem.Comparer.SortBy = header;
                listViewSortItem.Comparer.SortDirection = sortDirection;
                listViewSortItem.Comparer.DirectionDig = ((sortDirection.Equals(ListSortDirection.Ascending)) ? -1 : 1);

                try { collectionView.CustomSort = listViewSortItem.Comparer; }
                catch { }

                view.Items.Refresh();
                view.Cursor = System.Windows.Input.Cursors.Arrow;
            }
            else
            {
                view.Items.SortDescriptions.Clear();
                view.Items.SortDescriptions.Add(new SortDescription(headerClicked.Column.Header.ToString(), sortDirection));
                view.Items.Refresh();
            }

            listViewSortItem.LastColumnHeaderClicked = headerClicked;
            listViewSortItem.LastSortDirection = sortDirection;

            if ((listViewSortItem.Adorner != null) && (listViewSortItem.LastColumnHeaderClicked != null))
                AdornerLayer.GetAdornerLayer(listViewSortItem.LastColumnHeaderClicked).Remove(listViewSortItem.Adorner);

            switch (sortDirection)
            {
                case ListSortDirection.Ascending:
                    listViewSortItem.Adorner = new SortAdorner(headerClicked, ListSortDirection.Ascending);
                    AdornerLayer.GetAdornerLayer(headerClicked).Add(listViewSortItem.Adorner);
                    break;

                case ListSortDirection.Descending:
                    listViewSortItem.Adorner = new SortAdorner(headerClicked, ListSortDirection.Descending);
                    AdornerLayer.GetAdornerLayer(headerClicked).Add(listViewSortItem.Adorner);
                    break;
            }
        }

        private static void OnRegisterSortableGrid(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            if ((bool)(DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof(DependencyObject)).DefaultValue)) return;

            ListView listView = obj as ListView;

            if ((listView != null) && !_listViewDefinitions.ContainsKey(listView.Name))
            {
                try
                {
                    _listViewDefinitions.Add(listView.Name, new ListViewSortItem(Activator.CreateInstance(Type.GetType(GetCustomListViewSorter(obj))) as IListViewCustomComparer, null, ListSortDirection.Ascending));
                    listView.AddHandler(GridViewColumnHeader.ClickEvent, new RoutedEventHandler(GridViewColumnHeaderClickedHandler));
                }
                catch { }
            }
        }

        private static ListSortDirection GetSortingDirection(GridViewColumnHeader headerClicked, ListViewSortItem listViewSortItem)
        {
            if (headerClicked != listViewSortItem.LastColumnHeaderClicked) return ListSortDirection.Ascending;
            else
                return (listViewSortItem.LastSortDirection == ListSortDirection.Ascending) ? ListSortDirection.Descending : ListSortDirection.Ascending;
        }
    }
}