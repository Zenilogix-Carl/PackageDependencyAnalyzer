using System;
using System.Threading.Tasks;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Builders
{
    public class PackageCacheBuilder
    {
        private readonly IPackageCache _cache;
        private readonly PackageBuilder _packageBuilder;
        private readonly PackageVersionBuilder _packageVersionBuilder;

        public PackageCacheBuilder(IPackageCache cache, PackageBuilder packageBuilder, PackageVersionBuilder packageVersionBuilder)
        {
            _cache = cache;
            _packageBuilder = packageBuilder;
            _packageVersionBuilder = packageVersionBuilder;
        }

        /// <summary>
        /// Initializes the cache
        /// </summary>
        public void InitializeCache()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Gets a package as specified
        /// </summary>
        /// <param name="packageName"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        public async Task<IPackageVersion> GetPackageVersion(string packageName, ReleaseVersion version)
        {
            if (packageName == null) throw new ArgumentNullException(nameof(packageName));
            if (version == null) throw new ArgumentNullException(nameof(version));

            await Task.Run(() => { });
            return null;
        }


        public IPackage GetPackage(string packageName)
        {
            if (packageName == null) throw new ArgumentNullException(nameof(packageName));
            return null;
        }
    }
}
