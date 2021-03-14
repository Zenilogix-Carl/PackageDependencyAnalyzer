using System;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Analyzers
{
    public static class ExtensionMethods
    {
        public static bool Matches(this Version packageVersion, Version referenceVersion)
        {
            return packageVersion == referenceVersion ||
                    packageVersion.Major == referenceVersion.Major
                    && packageVersion.Minor == referenceVersion.Minor
                    && (packageVersion.Build == referenceVersion.Build || packageVersion.Build == -1 || referenceVersion.Build == -1)
                    && (packageVersion.Revision == referenceVersion.Revision || referenceVersion.Revision == -1 || packageVersion.Revision == -1);
        }

        public static bool Matches(this ReleaseVersion packageVersion, ReleaseVersion referenceVersion)
        {
            if (packageVersion.IsPrerelease || referenceVersion.IsPrerelease)
            {
                return packageVersion.Version == referenceVersion.Version &&
                       packageVersion.Release == referenceVersion.Release;
            }
            else
            {
                return packageVersion.Version.Matches(referenceVersion.Version);
            }
        }
    }
}
