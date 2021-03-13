using System;

namespace PackageDependencyAnalysis.Model
{
    public class BindingRedirection
    {
        public string AssemblyName { get; set; }
        public Version OldVersionFrom { get; set; }
        public Version OldVersionTo { get; set; }
        public Version NewVersion { get; set; }

        public BindingRedirection Clone()
        {
            return new BindingRedirection
            {
                AssemblyName = AssemblyName,
                OldVersionTo = OldVersionTo,
                OldVersionFrom = OldVersionFrom,
                NewVersion = NewVersion
            };
        }
    }
}
