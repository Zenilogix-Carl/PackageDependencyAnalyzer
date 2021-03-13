using System.Collections.Generic;

namespace PackageDependencyAnalysis.Model
{
    public class AssemblyInfo
    {
        public string Name { get; set; }

        public ICollection<string> Frameworks { get; set; }
    }
}
