using System;
using System.Collections.Generic;
using System.Linq;
using PackageDependencyAnalysis.Model;

namespace TestHarness.Models
{
    /// <summary>
    /// A specific version of a package
    /// </summary>
    public class PackageVersion : IPackageVersion
    {
        public IPackage Package { get; set; }

        public Version Version { get; set; }
        public DateTime DateTime { get; set; }

        public string Description { get; set; }

        public bool IsPrerelease { get; set; }
        public string PreReleaseSuffix { get; set; }

        public string File { get; set; }

        public PlatformDependencies Dependencies { get; } = new PlatformDependencies();

        public ICollection<IProject> ReferencingProjects { get; } = new List<IProject>();
        public ICollection<IProject> ConfigReferences { get; } = new List<IProject>();
        public ICollection<IProject> BindingRedirectReferences { get; } = new List<IProject>();

        public ICollection<IPackageVersion> ReferencingPackages { get; } = new List<IPackageVersion>();
        public string NuSpec { get; set; }
        public string RepositoryUrl { get; set; }
        public bool IsReferenced { get; set; }

        public override string ToString() => $"{Package.Name} {Version} ({Dependencies.SelectMany(s => s.Value).Count()} dependencies)";
    }
}
