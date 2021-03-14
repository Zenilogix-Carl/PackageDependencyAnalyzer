using System;
using System.Collections.Generic;
using System.Linq;

namespace PackageDependencyAnalysis.Model
{
    public class PackageReference
    {
        public IPackage Package { get; set; }
        public ReleaseVersion Version { get; set; }
        public IList<AssemblyReference> AssemblyReferences { get; set; }
        public bool IsRange { get; set; }
        public Version PackagesConfigVersion { get; set; }
        public IPackageVersion ResolvedReference { get; set; }
        public string OriginalXml { get; set; }

        public int? LineNumber { get; set; }
        public int? PackagesConfigLineNumber { get; set; }

        internal bool Visited { get; set; }

        public PackageReference Clone()
        {
            return new PackageReference
            {
                Package = Package,
                Version = Version,
                AssemblyReferences = AssemblyReferences.Select(a => a.Clone()).ToList(),
                PackagesConfigVersion = PackagesConfigVersion,
                ResolvedReference = ResolvedReference,
                IsRange = IsRange
            };
        }

        public override string ToString() => $"{Package.Name} {Version} {(ResolvedReference==null?"(unresolved)":$"=> {ResolvedReference.Version}")}";
    }
}
