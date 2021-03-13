using System.Collections.Generic;
using System.Collections.Specialized;

namespace PackageDependencyAnalysis.Model
{
    public interface IObservableCollection<T> : ICollection<T>,INotifyCollectionChanged
    {
    }
}
