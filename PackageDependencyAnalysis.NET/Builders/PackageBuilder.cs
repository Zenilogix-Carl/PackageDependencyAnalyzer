using System;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Builders
{
    public class PackageBuilder
    {
        private readonly IPackageCache _packageCache;
        private readonly Func<string, IPackage> _packageFactoryFunc;

        public PackageBuilder(IPackageCache packageCache, Func<string,IPackage> packageFactoryFunc)
        {
            _packageCache = packageCache ?? throw new ArgumentNullException(nameof(packageCache));
            _packageFactoryFunc = packageFactoryFunc ?? throw new ArgumentNullException(nameof(packageFactoryFunc));
        }

        public IPackage CreatePackage(string packageName)
        {
            if (packageName == null) throw new ArgumentNullException(nameof(packageName));

            var package = _packageFactoryFunc(packageName);

            _packageCache.PackagesDictionary[packageName] = package;

            return package;
        }

        public IPackage GetOrCreatePackage(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));

            if (!_packageCache.PackagesDictionary.TryGetValue(name, out var package))
            {
                package = CreatePackage(name);
            }

            return package;
        }
    }
}
