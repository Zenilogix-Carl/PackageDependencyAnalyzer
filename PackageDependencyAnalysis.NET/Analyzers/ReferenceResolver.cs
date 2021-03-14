using System;
using System.Linq;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Analyzers
{
    public class ReferenceResolver
    {
        private readonly ISolution _solution;
        private readonly IPackageCache _packageCache;
        private readonly Action<IContext,string> _logWarning;

        public ReferenceResolver(ISolution solution, IPackageCache packageCache, Action<IContext, string> logWarning)
        {
            _solution = solution;
            _packageCache = packageCache;
            _logWarning = logWarning;
        }

        public void ResolveAllReferences()
        {
            ResolveProjectPackageReferences();
            ResolvePackageReferences();
        }

        private void ResolveProjectPackageReferences()
        {
            foreach (var project in _solution.ProjectCache.Values)
            {
                foreach (var packageReference in project.PackageReferences)
                {
                    packageReference.Package.ReferencingProjects[project.Name] = project;

                    if (packageReference.ResolvedReference == null)
                    {
                        if (ResolvePackageReference(project, packageReference))
                        {
                            packageReference.ResolvedReference?.ReferencingProjects.Add(project);
                        }
                    }

                    //if (packageReference.BindingRedirection != null)
                    //{
                    //    ResolveBindingRedirection(project, packageReference.BindingRedirection);
                    //}

                    if (packageReference.PackagesConfigVersion != null)
                    {
                        ResolvePackagesConfig(project, packageReference);
                    }
                }
            }
        }

        private void ResolvePackageReferences()
        {
            foreach (var packageCachePackage in _packageCache.Packages)
            {
                foreach (var packageVersion in packageCachePackage.PackageVersions)
                {
                    if (packageVersion != null)
                    {
                        foreach (var dependency in packageVersion.Dependencies.Values)
                        {
                            foreach (var packageReference in dependency.Values.Where(p => p.ResolvedReference == null))
                            {
                                packageReference.Package.ReferencingPackages[packageVersion.Package.Name] = packageVersion;

                                if (ResolvePackageReference(packageVersion, packageReference))
                                {
                                    packageReference.ResolvedReference.ReferencingPackages.Add(packageVersion);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resolves a package reference
        /// </summary>
        /// <param name="context"></param>
        /// <param name="packageReference"></param>
        private bool ResolvePackageReference(IContext context, PackageReference packageReference)
        {
            var package = packageReference.Package;

            if (package.VersionDictionary.TryGetValue(packageReference.Version, out var packageVersion))
            {
                packageReference.ResolvedReference = packageVersion;
                return true;
            }
            else
            {
                var match = package.Versions.SingleOrDefault(v => v.Matches(packageReference.Version));
                if (match != null)
                {
                    packageReference.ResolvedReference = package.VersionDictionary[match];
                    return true;
                }
            }

            _logWarning(context,
                $"Could not resolve package reference {packageReference.Package.Name} {packageReference.Version}; available versions: {string.Join(",", package.Versions)}");
            return false;
        }

        private void ResolveBindingRedirection(IProject project, BindingRedirection bindingRedirection)
        {
            if (_packageCache.PackagesDictionary.TryGetValue(bindingRedirection.AssemblyName, out var package))
            {
                if (package.VersionDictionary.TryGetValue(bindingRedirection.NewVersion, out var packageVersion))
                {
                    packageVersion.BindingRedirectReferences.Add(project);
                }
                else
                {
                    var match = package.Versions.SingleOrDefault(v => v.Matches(bindingRedirection.NewVersion));
                    if (match != null)
                    {
                        package.VersionDictionary[match].BindingRedirectReferences.Add(project);
                    }
                }
            }
        }

        public void ResolvePackagesConfig(IProject project, PackageReference packageReference)
        {
            if (packageReference.Package.VersionDictionary.TryGetValue(packageReference.PackagesConfigVersion, out var packageVersion))
            {
                if (packageReference.ResolvedReference == null)
                {
                    packageReference.ResolvedReference = packageVersion;
                }
                packageVersion.ConfigReferences.Add(project);
            }
        }
    }
}
