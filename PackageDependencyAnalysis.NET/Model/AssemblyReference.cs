using System;

namespace PackageDependencyAnalysis.Model
{
    public class AssemblyReference
    {
        public IPackage Package { get; set; }
        public Version Version { get; set; }
        public BindingRedirection BindingRedirection { get; set; }
        public string HintPath { get; set; }

        public AssemblyReference Clone()
        {
            return new AssemblyReference
            {
                Package = Package,
                Version = Version,
                BindingRedirection = BindingRedirection.Clone(),
                HintPath = HintPath
            };
        }
    }
}
