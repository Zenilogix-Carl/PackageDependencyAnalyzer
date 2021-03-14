using System;
using System.Threading.Tasks;
using PackageDependencyAnalysis.Model;
using PackageDependencyAnalysis.Repository;

namespace PackageDependencyAnalysis.Builders
{
    public class PackageVersionBuilder
    {
        private readonly Func<IPackage, ReleaseVersion, IPackageVersion> _packageVersionFactoryFunc;
        private readonly PackageBuilder _packageBuilder;
        private readonly PackageRepository _packageRepository;

        public PackageVersionBuilder(Func<IPackage, ReleaseVersion, IPackageVersion> packageVersionFactoryFunc, PackageBuilder packageBuilder, PackageRepository packageRepository)
        {
            _packageVersionFactoryFunc = packageVersionFactoryFunc ?? throw new ArgumentNullException(nameof(packageVersionFactoryFunc));
            _packageBuilder = packageBuilder ?? throw new ArgumentNullException(nameof(packageBuilder));
            _packageRepository = packageRepository ?? throw new ArgumentNullException(nameof(packageRepository));
        }

        public async Task<IPackageVersion> CreatePackageVersion(IPackage package, ReleaseVersion version)
        {
            if (package == null) throw new ArgumentNullException(nameof(package));
            if (version == null) throw new ArgumentNullException(nameof(version));

            var packageVersion = _packageVersionFactoryFunc(package, version);

            var packageContext = await _packageRepository.Find(package.Name, version.Version, version.Release);

            packageVersion.Source = packageContext.Source;
            packageVersion.DateTime = packageContext.DateTime;
            packageVersion.Description = packageContext.Description;
            packageVersion.NuSpec = packageContext.RawNuSpec;

            return packageVersion;
        }

        public async Task<IPackageVersion> GetOrCreatePackageVersion(string name, ReleaseVersion version)
        {
            var package = _packageBuilder.GetOrCreatePackage(name);
            if (!package.VersionDictionary.TryGetValue(version, out var packageVersion))
            {
                packageVersion = await CreatePackageVersion(package, version);
                package.VersionDictionary[version] = packageVersion;
            }

            return packageVersion;
        }
    }
}
