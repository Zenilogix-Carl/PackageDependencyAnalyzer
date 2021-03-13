using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PackageDependencyAnalysis.RemoteServices
{
    public class NuGetSources
    {
        private readonly NuGetSource[] _sources;

        public NuGetSources(IEnumerable<string> sources)
        {
            _sources = sources.Select(s => new NuGetSource(s)).ToArray();
        }

        public NuGetSources(IEnumerable<NuGetSource> sources)
        {
            _sources = sources.ToArray();
        }

        public async Task<IEnumerable<NuGetSource.PackageVersion>> GetPackageVersions(string packageName,
            bool includePreRelease)
        {
            var tasks = _sources.Select(s => s.GetPackageVersions(packageName, includePreRelease));
            var result = await Task.WhenAll(tasks);
            return result.SelectMany(s => s);
        }

        public async Task<IEnumerable<NuGetSource.PackageMetaData>> GetPackageMetaData(string packageName,
            bool includePreRelease)
        {
            var tasks = _sources.Select(s => s.GetPackageMetaData(packageName, includePreRelease));
            var result = await Task.WhenAll(tasks);
            return result.SelectMany(s => s);
        }
    }
}
