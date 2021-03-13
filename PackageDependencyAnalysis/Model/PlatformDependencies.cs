using System.Collections.Generic;

namespace PackageDependencyAnalysis.Model
{
    /// <summary>
    /// Dictionary of package dependencies by platform name
    /// </summary>
    public class PlatformDependencies : Dictionary<string, Dependencies>
    {
    }
}
