using System;
using System.Collections.Generic;

namespace PackageDependencyAnalysis.ContextObjects
{
    public interface IAssemblyDetailsContext
    {
        string AssemblyName { get; set; }
        Version AssemblyVersion { get; set; }

        Dictionary<string, string> AssemblyProperties { get; }
    }
}
