using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PackageDependencyAnalysis.Model;

namespace TestHarness.Models
{
    public class Project : IProject
    {
        public string Name { get; set; }
        public string AbsolutePath { get; set; }
        public Version Version { get; set; }
        public ICollection<IProject> Projects { get; } = new ObservableCollection<IProject>();

        public IDictionary<string, PackageReference> PackageReferenceDictionary { get; } =
            new Dictionary<string, PackageReference>();
        public ICollection<PackageReference> PackageReferences { get; } = new List<PackageReference>();
        public string File => AbsolutePath;

        public override string ToString() => $"{Name} ({Projects.Count} project references, {PackageReferences.Count} package references)";
    }
}
