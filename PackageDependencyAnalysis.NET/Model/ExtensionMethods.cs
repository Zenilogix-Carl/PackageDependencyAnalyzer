using System;
using System.Collections.Generic;

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
        /// <param name="comparer">Comparer function</param>
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
        /// <param name="comparer">Comparer function</param>
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
    }
}
