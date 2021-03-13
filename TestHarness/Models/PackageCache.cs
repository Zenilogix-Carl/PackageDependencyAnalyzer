using System.Collections.Generic;
using PackageDependencyAnalysis.Model;

namespace TestHarness.Models
{
    public class PackageCache : IPackageCache
    {
        public IDictionary<string, IPackage> PackagesDictionary { get; } = new Dictionary<string, IPackage>();

        public ICollection<IPackage> Packages => PackagesDictionary.Values;

        public void Clear()
        {
            PackagesDictionary.Clear();
        }
    }
}
