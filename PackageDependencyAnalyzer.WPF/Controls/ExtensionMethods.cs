using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace PackageDependencyAnalyzer.Controls
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Apply sort to contents of an <see cref="ItemsControl"/>
        /// </summary>
        /// <param name="control">Control</param>
        /// <param name="propertyName">Property name. Dot notation allowed</param>
        /// <param name="direction">Sort direction</param>
        /// <remarks>Use RegisterSort if <see cref="ItemsControl.ItemsSource"/> is set dynamically
        /// </remarks>
        public static void SortBy(this ItemsControl control, string propertyName, ListSortDirection direction=ListSortDirection.Ascending)
        {
            var view = (CollectionView)CollectionViewSource.GetDefaultView(control.ItemsSource);
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(propertyName, direction));
        }

        /// <summary>
        /// Apply sort to contents of an <see cref="ItemsControl"/>
        /// </summary>
        /// <param name="control">Control</param>
        /// <param name="propertyName">Property name (separate components of a dotted name expression)</param>
        /// <param name="direction">Sort direction</param>
        /// <remarks>Use RegisterSort if <see cref="ItemsControl.ItemsSource"/> is set dynamically
        /// </remarks>
        public static void SortBy(this ItemsControl control, ListSortDirection direction, params string[] propertyName)
        {
            SortBy(control, string.Join(".", propertyName), direction);
        }

        /// <summary>
        /// Apply sort to contents of an <see cref="ItemsControl"/>
        /// </summary>
        /// <param name="control">Control</param>
        /// <param name="propertyName">Property name (separate components of a dotted name expression)</param>
        /// <remarks>Use RegisterSort if <see cref="ItemsControl.ItemsSource"/> is set dynamically
        /// </remarks>
        public static void SortBy(this ItemsControl control, params string[] propertyName)
        {
            SortBy(control, string.Join(".", propertyName));
        }

        /// <summary>
        /// Registers a sort on a control where <see cref="ItemsControl.ItemsSource"/> is dynamically assigned
        /// </summary>
        /// <typeparam name="TItemsControl">Type of control</typeparam>
        /// <param name="control">Control</param>
        /// <param name="propertyName">Item property name. Dot notation allowed</param>
        /// <param name="direction">Sort direction</param>
        /// <remarks>Use SortBy if <see cref="ItemsControl.ItemsSource"/> is not set dynamically
        /// </remarks>
        public static void RegisterSort<TItemsControl>(this TItemsControl control, string propertyName, ListSortDirection direction=ListSortDirection.Ascending)
            where TItemsControl : ItemsControl
        {
            var type = typeof(TItemsControl);

            var dpd = DependencyPropertyDescriptor.FromProperty(ItemsControl.ItemsSourceProperty, type);
            dpd?.AddValueChanged(control, (o, eventArgs) =>
            {
                if (control.ItemsSource != null)
                {
                    control.SortBy(propertyName, direction);
                }
            });
        }

        /// <summary>
        /// Registers a sort on a control where <see cref="ItemsControl.ItemsSource"/> is dynamically assigned
        /// </summary>
        /// <typeparam name="TItemsControl">Type of control</typeparam>
        /// <param name="control">Control</param>
        /// <param name="propertyName">Property name (separate components of a dotted name expression)</param>
        /// <param name="direction">Sort direction</param>
        /// <remarks>Use SortBy if <see cref="ItemsControl.ItemsSource"/> is not set dynamically
        /// </remarks>
        public static void RegisterSort<TItemsControl>(this TItemsControl control, ListSortDirection direction, params string[] propertyName)
            where TItemsControl : ItemsControl
        {
            RegisterSort(control, string.Join(".", propertyName), direction);
        }

        /// <summary>
        /// Registers a sort on a control where <see cref="ItemsControl.ItemsSource"/> is dynamically assigned
        /// </summary>
        /// <typeparam name="TItemsControl">Type of control</typeparam>
        /// <param name="control">Control</param>
        /// <param name="propertyName">Property name (separate components of a dotted name expression)</param>
        /// <remarks>Use SortBy if <see cref="ItemsControl.ItemsSource"/> is not set dynamically
        /// </remarks>
        public static void RegisterSort<TItemsControl>(this TItemsControl control, params string[] propertyName)
            where TItemsControl : ItemsControl
        {
            RegisterSort(control, string.Join(".", propertyName));
        }
    }
}
