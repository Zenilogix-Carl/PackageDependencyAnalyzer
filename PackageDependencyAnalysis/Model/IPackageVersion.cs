using System;
using System.Collections.Generic;

namespace PackageDependencyAnalysis.Model
{
    public interface IPackageVersion : IContext
    {
        IPackage Package { get; set; }

        Version Version { get; set; }

        DateTime DateTime { get; set; }

        string Description { get; set; }

        bool IsPrerelease { get; set; }
        string PreReleaseSuffix { get; set; }

        ICollection<AssemblyInfo> Assemblies { get; }

        PlatformDependencies Dependencies { get; }

        ICollection<IProject> ReferencingProjects { get; }

        ICollection<IProject> ConfigReferences { get; }

        ICollection<IProject> BindingRedirectReferences { get; }

        ICollection<IPackageVersion> ReferencingPackages { get; }

        string NuSpec { get; set; }
        string RepositoryUrl { get; set; }

        /// <summary>
        /// Marks a package as dependency of the current solution, either directly or indirectly
        /// </summary>
        bool IsReferenced { get; set; }
    }
}
