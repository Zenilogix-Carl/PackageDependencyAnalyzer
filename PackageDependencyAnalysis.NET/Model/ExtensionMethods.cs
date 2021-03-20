using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace PackageDependencyAnalysis.Model
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Sorted insert using binary search algorithm
        /// </summary>
        /// <typeparam name="T">Type of object in the collection</typeparam>
        /// <param name="collection">Collection to receive inserted item</param>
        /// <param name="item">Item to be inserted</param>
        /// <param name="comparer">Comparer function; returns a value in the manner of <see cref="IComparable.CompareTo"/></param>
        public static void SortedInsert<T>(this IList<T> collection, T item, Func<T,T,int> comparer)
        {
            collection.Insert(BinarySearch(collection, item, comparer), item);
        }

        /// <summary>
        /// Binary search for item in collection
        /// </summary>
        /// <typeparam name="T">Type of object in the collection</typeparam>
        /// <param name="collection">Collection to receive inserted item</param>
        /// <param name="item">Item to be inserted</param>
        /// <param name="comparer">Comparer function; returns a value in the manner of <see cref="IComparable.CompareTo"/></param>
        /// <returns>Returns the index of the item's proper place in the collection; if the item does not occur in the collection
        /// the index will correspond to its appropriate insertion point, which may be after the last existing item
        /// </returns>
        public static int BinarySearch<T>(this IList<T> collection, T item, Func<T, T, int> comparer)
        {
            // Set up pos to be smallest power of 2 greater than or equal to size of collection
            var pos = 1;
            while (pos < collection.Count)
            {
                pos <<= 1;
            }

            // Initial increment; half of pos, but not less than 1
            var inc = pos > 1 ? pos / 2 : 1;

            // Finalize starting value of pos to be power of 2 minus 1
            if (pos > 1)
            {
                pos--;
            }

            // Main loop to converge on item having closest sort key
            while (inc > 0)
            {
                if (pos >= collection.Count || comparer(item, collection[pos]) < 0)
                {
                    pos -= inc;
                }
                else
                {
                    pos += inc;
                }

                inc /= 2;
            }

            // Adjustment to place position on correct side of nearest neighbor
            if (collection.Count > pos && comparer(item, collection[pos]) > 0)
            {
                pos++;
            }

            return pos;
        }

        /// <summary>
        /// Sorted insert using binary search algorithm
        /// </summary>
        /// <typeparam name="TItem">Type of item in collection</typeparam>
        /// <typeparam name="TKey">Type of key</typeparam>
        /// <param name="collection">Collection to receive inserted item</param>
        /// <param name="item">Item to be inserted</param>
        /// <param name="keySelector">Key selector function</param>
        /// <param name="direction">Sort direction</param>
        public static void SortedInsert<TItem, TKey>(this IList<TItem> collection, TItem item, Func<TItem, TKey> keySelector, ListSortDirection direction=ListSortDirection.Ascending)
            where TKey : IComparable
        {
            collection.Insert(BinarySearch(collection, item, keySelector, direction), item);
        }

        /// <summary>
        /// Binary search for item in collection
        /// </summary>
        /// <typeparam name="TItem">Type of item in collection</typeparam>
        /// <typeparam name="TKey">Type of key</typeparam>
        /// <param name="collection">Collection to receive inserted item</param>
        /// <param name="item">Item to be inserted</param>
        /// <param name="keySelector">Key selector function</param>
        /// <param name="direction">Sort direction</param>
        /// <returns>Returns the index of the item's proper place in the collection; if the item does not occur in the collection
        /// the index will correspond to its appropriate insertion point, which may be after the last existing item
        /// </returns>
        public static int BinarySearch<TItem, TKey>(this IList<TItem> collection, TItem item, Func<TItem, TKey> keySelector, ListSortDirection direction=ListSortDirection.Ascending)
            where TKey : IComparable
        {
            switch (direction)
            {
                case ListSortDirection.Ascending:
                    return BinarySearch(collection, item,
                        (item1, item2) => keySelector(item1).CompareTo(keySelector(item2)));
                case ListSortDirection.Descending:
                    return BinarySearch(collection, item,
                        (item1, item2) => -keySelector(item1).CompareTo(keySelector(item2)));
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        /// <summary>
        /// Sorted insert using binary search algorithm
        /// </summary>
        /// <typeparam name="TItem">Type of item in collection</typeparam>
        /// <param name="collection">Collection to receive inserted item</param>
        /// <param name="item">Item to be inserted</param>
        /// <param name="direction">Sort direction</param>
        public static void SortedInsert<TItem>(this IList<TItem> collection, TItem item, ListSortDirection direction = ListSortDirection.Ascending)
            where TItem : IComparable
        {
            collection.Insert(BinarySearch(collection, item, direction), item);
        }

        /// <summary>
        /// Binary search for item in collection
        /// </summary>
        /// <typeparam name="TItem">Type of item in collection</typeparam>
        /// <param name="collection">Collection to receive inserted item</param>
        /// <param name="item">Item to be inserted</param>
        /// <param name="direction">Sort direction</param>
        /// <returns>Returns the index of the item's proper place in the collection; if the item does not occur in the collection
        /// the index will correspond to its appropriate insertion point, which may be after the last existing item
        /// </returns>
        public static int BinarySearch<TItem>(this IList<TItem> collection, TItem item, ListSortDirection direction = ListSortDirection.Ascending)
            where TItem : IComparable
        {
            return BinarySearch(collection, item, a => a, direction);
        }
    }
}
