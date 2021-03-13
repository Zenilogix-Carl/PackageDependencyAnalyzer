using System;

namespace PackageDependencyAnalysis.Model
{
    public class PackageReference
    {
        public IPackage Package { get; set; }
        public Version Version { get; set; }
        public Version PackagesConfigVersion { get; set; }
        public BindingRedirection BindingRedirection { get; set; }
        public IPackageVersion ResolvedReference { get; set; }
        public bool IsPrerelease { get; set; }
        public string PreReleaseSuffix { get; set; }
        public bool IsRange { get; set; }
        public string HintPath { get; set; }

        internal bool Visited { get; set; }

        public PackageReference Clone()
        {
            return new PackageReference
            {
                Package = Package,
                Version = Version,
                PackagesConfigVersion = PackagesConfigVersion,
                BindingRedirection = BindingRedirection?.Clone(),
                ResolvedReference = ResolvedReference,
                IsPrerelease = IsPrerelease,
                PreReleaseSuffix = PreReleaseSuffix,
                IsRange = IsRange,
                HintPath = HintPath
            };
        }

        public override string ToString() => $"{Package.Name} {Version} {(ResolvedReference==null?"(unresolved)":$"=> {ResolvedReference.Version}")}";
    }
}
