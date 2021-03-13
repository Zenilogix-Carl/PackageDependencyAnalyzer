using System.Collections.Generic;
using System.Linq;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Analyzers
{
    public class IssueScanner
    {
        public static IEnumerable<string> Scan(IPackageCache packageCache)
        {
            foreach (var package in packageCache.Packages)
            {
                if (package.PackageVersions.Count(v => v.IsReferenced) > 1)
                {
                    yield return
                        $"{package.Name}: multiple versions in use: {string.Join(", ", package.PackageVersions.Where(v => v.IsReferenced).Select(v => v.Version.ToString()))}";
                }
            }
        }
    }
}
