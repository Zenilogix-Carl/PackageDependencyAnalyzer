using System.Collections.Generic;

namespace PackageDependencyAnalysis.Model
{
    public interface IPackageCache
    {
        /// <summary>
        /// All packages associated with solution
        /// </summary>
        IDictionary<string, IPackage> PackagesDictionary { get; }
        ICollection<IPackage> Packages { get; }
        void Clear();
    }
}
