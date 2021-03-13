using System;

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
    }
}
