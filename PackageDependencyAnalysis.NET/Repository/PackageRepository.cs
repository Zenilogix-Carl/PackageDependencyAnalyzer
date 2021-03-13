using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PackageDependencyAnalysis.ContextObjects;
using PackageDependencyAnalysis.Model;
using PackageDependencyAnalysis.RemoteServices;

namespace PackageDependencyAnalysis.Repository
{
    public class PackageRepository
    {
        private readonly Func<string, bool> _frameworkFilter;
        private readonly string _solutionCacheFolder;
        private readonly string _userPackagesFolder;
        private readonly NuGetSources _nugetSources;

        public PackageRepository(string solutionFileName, IEnumerable<string> nugetSources, Func<string,bool> frameworkFilter=null)
        {
            _frameworkFilter = frameworkFilter;
            _solutionCacheFolder = Path.Combine(Path.GetDirectoryName(solutionFileName), "packages");
            _userPackagesFolder = Path.Combine(Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"), @".nuget\packages");
            _nugetSources = new NuGetSources(nugetSources);
        }

        public async Task<PackageContext> Find(string name, Version version, string releaseLabel)
        {
            var suffix = string.IsNullOrEmpty(releaseLabel) ? string.Empty : $"-{releaseLabel}";
            var fileName = $"{name}.{version}{suffix}.nupkg";

            if (Directory.Exists(_solutionCacheFolder))
            {
                foreach (var enumerateFile in Directory.EnumerateFiles(_solutionCacheFolder, fileName, SearchOption.AllDirectories))
                {
                    return PackageContext.Create(enumerateFile, _frameworkFilter);
                }
            }

            if (Directory.Exists(_userPackagesFolder))
            {
                foreach (var enumerateFile in Directory.EnumerateFiles(_userPackagesFolder, fileName, SearchOption.AllDirectories))
                {
                    return PackageContext.Create(enumerateFile, _frameworkFilter);
                }
            }

            var versions = await _nugetSources.GetPackageVersions(name, !string.IsNullOrWhiteSpace(releaseLabel));
            foreach (var packageVersion in versions)
            {
                if (packageVersion.Version == version && packageVersion.Release == (releaseLabel ?? string.Empty))
                {
                    using (var stream = await packageVersion.Source.GetPackage(name, version, releaseLabel))
                    {
                        return new PackageContext(stream, _frameworkFilter);
                    }
                }
            }

            return null;
        }
    }
}
