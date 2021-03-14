using System;
using System.Threading.Tasks;
using PackageDependencyAnalysis.ContextObjects;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Builders
{
    public class PackageReferenceBuilder
    {
        private readonly PackageVersionBuilder _packageVersionBuilder;
        private readonly PackageBuilder _packageBuilder;

        public PackageReferenceBuilder(PackageVersionBuilder packageVersionBuilder, PackageBuilder packageBuilder)
        {
            _packageVersionBuilder = packageVersionBuilder ?? throw new ArgumentNullException(nameof(packageVersionBuilder));
            _packageBuilder = packageBuilder ?? throw new ArgumentNullException(nameof(packageBuilder));
        }

        public async Task<PackageReference> CreatePackageReference(PackageReferenceContext packageReferenceContext)
        {
            var packageVersion = await _packageVersionBuilder.GetOrCreatePackageVersion(packageReferenceContext.Name, packageReferenceContext.Version);
            if (packageVersion != null)
            {
                return new PackageReference
                {
                    Package = packageVersion.Package,
                    Version = packageReferenceContext.Version,
                    LineNumber = packageReferenceContext.LineNumber,
                    OriginalXml = packageReferenceContext.OriginalXml
                };
            }

            return new PackageReference
            {
                Package = _packageBuilder.GetOrCreatePackage(packageReferenceContext.Name),
                Version = packageReferenceContext.Version,
                LineNumber = packageReferenceContext.LineNumber,
                OriginalXml = packageReferenceContext.OriginalXml
            };
        }

        public async Task<PackageReference> CreatePackageReference(PackagesConfigPackageContext packageReferenceContext)
        {
            var packageVersion = await _packageVersionBuilder.GetOrCreatePackageVersion(packageReferenceContext.Id, packageReferenceContext.Version);
            if (packageVersion != null)
            {
                return new PackageReference
                {
                    Package = packageVersion.Package,
                    Version = packageReferenceContext.Version,
                    LineNumber = packageReferenceContext.LineNumber,
                    OriginalXml = packageReferenceContext.OriginalXml
                };
            }

            return new PackageReference
            {
                Package = _packageBuilder.GetOrCreatePackage(packageReferenceContext.Id),
                Version = packageReferenceContext.Version,
                LineNumber = packageReferenceContext.LineNumber,
                OriginalXml = packageReferenceContext.OriginalXml
            };
        }
    }
}
