using System;

namespace PackageDependencyAnalysis.ContextObjects
{
    public interface IAssemblyReferenceContext
    {
        string Name { get; }
        Version Version { get; set; }
        string PreRelease { get; set; }
        bool IsPreRelease { get; }
    }
}
