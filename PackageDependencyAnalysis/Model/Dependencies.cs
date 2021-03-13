using System.Collections.Generic;

namespace PackageDependencyAnalysis.Model
{
    /// <summary>
    /// Dictionary of specific versions by package name
    /// </summary>
    public class Dependencies : Dictionary<string, PackageReference>
    {
    }
}
