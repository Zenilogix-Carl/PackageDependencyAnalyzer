using System.Collections.Generic;
using System.Linq;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Analyzers
{
    public class DependencyAnalyzer
    {
        public static ICollection<IPackage> GetReferencedPackages(IPackageCache packageCache)
        {
            foreach (var package in packageCache.Packages)
            {
                package.IsReferenced = false;
                foreach (var packageVersion in package.PackageVersions)
                {
                    packageVersion.IsReferenced = false;
                }
            }

            foreach (var package in packageCache.Packages.Where(p => p.ReferencingProjects.Any()))
            {
                package.IsReferenced = package.HasVersions;
                MarkReferences(package);
            }

            return packageCache.Packages.Where(p => p.IsReferenced).ToList();
        }

        private static void MarkReferences(IPackage package)
        {
            foreach (var packageVersion in package.PackageVersions)
            {
                if (packageVersion.ReferencingProjects.Any())
                {
                    packageVersion.IsReferenced = true;
                }

                foreach (var platform in packageVersion.Dependencies.Values)
                {
                    foreach (var dependency in platform.Values)
                    {
                        if (!dependency.Visited)
                        {
                            dependency.Visited = true;
                            dependency.Package.IsReferenced = dependency.Package.HasVersions;
                            if (dependency.ResolvedReference != null)
                            {
                                dependency.ResolvedReference.IsReferenced = true;
                            }
                            MarkReferences(dependency.Package);
                        }
                    }
                }
            }
        }

        public static ICollection<IPackage> GetDependentPackages(IPackage package)
        {
            var packageDictionary = new Dictionary<string, IPackage>();

            GetDependentPackages(packageDictionary, package);

            return packageDictionary.Values;
        }

        private static void GetDependentPackages(Dictionary<string, IPackage> packageDictionary, IPackage package)
        {
            foreach (var referencingPackage in package.ReferencingPackages)
            {
                if (!packageDictionary.ContainsKey(referencingPackage.Key))
                {
                    packageDictionary[referencingPackage.Key] = referencingPackage.Value.Package;
                    GetDependentPackages(packageDictionary, referencingPackage.Value.Package);
                }
            }
        }

        public static ICollection<IPackageVersion> GetDependentPackages(IPackageVersion package)
        {
            var packageDictionary = new Dictionary<string, IPackageVersion>();

            GetDependentPackages(packageDictionary, package);

            return packageDictionary.Values;
        }

        private static void GetDependentPackages(IDictionary<string, IPackageVersion> packageDictionary, IPackageVersion package)
        {
            foreach (var referencingPackage in package.ReferencingPackages)
            {
                if (!packageDictionary.ContainsKey(referencingPackage.Package.Name))
                {
                    packageDictionary[referencingPackage.Package.Name] = referencingPackage;
                    GetDependentPackages(packageDictionary, referencingPackage);
                }
            }
        }
    }
}
