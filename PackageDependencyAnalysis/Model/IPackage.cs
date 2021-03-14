using System;
using System.Collections.Generic;

namespace PackageDependencyAnalysis.Model
{
    public interface IPackage
    {
        string Name { get;  }

        ICollection<ReleaseVersion> Versions { get; }

        ICollection<IPackageVersion> PackageVersions { get; }

        IDictionary<ReleaseVersion, IPackageVersion> VersionDictionary { get; }

        IDictionary<string,IProject> ReferencingProjects { get; }
        IDictionary<string,IPackageVersion> ReferencingPackages { get; }

        bool IsReferenced { get; set; }
        bool HasVersions { get; }
    }
}
