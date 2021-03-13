using System;
using System.Collections.Generic;
using System.Linq;
using PackageDependencyAnalysis.Model;

namespace TestHarness.Models
{
    public class Package : IPackage
    {
        public string Name { get; set; }
        public ICollection<Version> Versions => VersionDictionary.Keys;
        public ICollection<IPackageVersion> PackageVersions => VersionDictionary.Values;

        public IDictionary<Version, IPackageVersion> VersionDictionary { get; } =
            new Dictionary<Version, IPackageVersion>();

        public IDictionary<string, IProject> ReferencingProjects => new Dictionary<string, IProject>();
        public IDictionary<string, IPackageVersion> ReferencingPackages => new Dictionary<string, IPackageVersion>();
        public bool IsReferenced { get; set; }
        public bool HasVersions => Versions.Any();

        public override string ToString() => $"{Name} ({VersionDictionary.Count} version(s)) {string.Join(", ", Versions)}";
    }
}
