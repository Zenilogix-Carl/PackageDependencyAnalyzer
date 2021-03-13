using System.Xml.Linq;

namespace PackageDependencyAnalysis.ContextObjects
{
    public class NuSpecDependencyGroup : NuSpecReferencesContext
    {
        private const string TargetFrameworkString = "targetFramework";

        public string TargetFramework => ReferencesElement.Attribute(TargetFrameworkString)?.Value ?? string.Empty;

        internal NuSpecDependencyGroup(XElement groupElement, XNamespace ns) : base(groupElement, ns)
        {
        }
    }
}
