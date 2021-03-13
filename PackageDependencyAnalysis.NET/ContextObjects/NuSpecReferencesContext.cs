using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace PackageDependencyAnalysis.ContextObjects
{
    public class NuSpecReferencesContext
    {
        private const string ReferenceString = "reference";
        private const string DependencyString = "dependency";

        public IEnumerable<NuSpecReference> References => (ReferencesElement.Elements(Ns + ReferenceString).Select(NuSpecReference.Create).Where(r => r != null)
            .Union(ReferencesElement.Elements(Ns + DependencyString).Select(NuSpecReference.Create).Where(r => r != null)));

        protected readonly XElement ReferencesElement;
        protected readonly XNamespace Ns;

        internal NuSpecReferencesContext(XElement referencesElement, XNamespace ns)
        {
            ReferencesElement = referencesElement;
            Ns = ns;
        }
    }
}
