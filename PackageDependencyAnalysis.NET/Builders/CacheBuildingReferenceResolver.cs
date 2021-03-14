using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PackageDependencyAnalysis.Analyzers;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Builders
{
    /// <summary>
    /// Builds package cache as it resolves references
    /// </summary>
    public class CacheBuildingReferenceResolver
    {
        private readonly ISolution _solution;
        private readonly IPackageCache _packageCache;
        private readonly Action<IContext, string> _logWarning;

        public CacheBuildingReferenceResolver(ISolution solution, IPackageCache packageCache, Action<IContext, string> logWarning)
        {
            _solution = solution ?? throw new ArgumentNullException(nameof(solution));
            _packageCache = packageCache ?? throw new ArgumentNullException(nameof(packageCache));
            _logWarning = logWarning ?? throw new ArgumentNullException(nameof(logWarning));
        }

        public async Task ResolveProjectPackageReferences()
        {
            var tasks = new List<Task>();

            foreach (var project in _solution.ProjectCache.Values)
            {
                foreach (var packageReference in project.PackageReferences)
                {
                    packageReference.Package.ReferencingProjects[project.Name] = project;

                    if (packageReference.ResolvedReference == null)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            if (await ResolvePackageReference(project, packageReference))
                            {
                                packageReference.ResolvedReference?.ReferencingProjects.Add(project);
                            }
                        }));
                    }

                    if (packageReference.PackagesConfigVersion != null)
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            await ResolvePackagesConfig(project, packageReference);
                        }));
                    }
                }
            }

            await Task.WhenAll(tasks);
        }

        private async Task<bool> ResolvePackageReference(IContext context, PackageReference packageReference)
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

        public async Task ResolvePackagesConfig(IProject project, PackageReference packageReference)
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
