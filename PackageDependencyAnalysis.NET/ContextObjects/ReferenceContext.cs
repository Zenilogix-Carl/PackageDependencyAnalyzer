using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace PackageDependencyAnalysis.ContextObjects
{
    internal class ReferenceContext : ReferenceContextBase, IAssemblyDetailsContext
    {
        private const string HintPathString = "HintPath";
        private const string VersionString = "Version";

        internal XElement HintPathElement { get; }

        public Dictionary<string,string> AssemblyProperties { get; }

        public override string Name { get; }

        public string AssemblyName { get; set; }
        public Version AssemblyVersion { get; set; }

        public string HintPath => HintPathElement.Value;

        public bool IsPackage => HintPathElement != null;

        internal static ReferenceContext Create(ProjectContext context, XElement element)
        {
            var referenceContext = new ReferenceContext(context, element);
            return referenceContext.AssemblyVersion == null ? null : referenceContext;
        }

        private ReferenceContext(ProjectContext context, XElement element) : base(context, element)
        {
            var includeParts = Include.Split(',');

            AssemblyName = includeParts.First();
            Name = AssemblyName;

            AssemblyProperties = includeParts.Skip(1).Select(s => s.Split('='))
                .ToDictionary(s => s[0].Trim(), s => s[1].Trim());

            if (AssemblyProperties.TryGetValue(VersionString, out var versionString))
            {
                AssemblyVersion = new Version(versionString);
            }

            HintPathElement = element.Element(Namespace + HintPathString);
        }
    }
}
