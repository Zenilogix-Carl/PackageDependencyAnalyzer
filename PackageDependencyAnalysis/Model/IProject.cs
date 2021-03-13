using System;
using System.Collections.Generic;

namespace PackageDependencyAnalysis.Model
{
    public interface IProject : IContext
    {
        string Name { get; set; }
        string AbsolutePath { get; set; }
        string PackagesConfigPath { get; set; }
        string AppConfigPath { get; set; }
        Version Version { get; set; }
        ICollection<IProject> Projects { get; }
        ICollection<IProject> Dependencies { get; }
        IDictionary<string,PackageReference> PackageReferenceDictionary { get; }
        ICollection<PackageReference> PackageReferences { get; }
        ICollection<BindingRedirection> BindingRedirections { get; }
    }
}
