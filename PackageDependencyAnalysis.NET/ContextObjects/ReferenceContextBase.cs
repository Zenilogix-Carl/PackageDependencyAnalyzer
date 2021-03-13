using System.Xml;
using System.Xml.Linq;

namespace PackageDependencyAnalysis.ContextObjects
{
    public abstract class ReferenceContextBase
    {
        private const string IncludeString = "Include";

        internal XElement Element { get; }
        protected string Include { get; }
        protected XNamespace Namespace => ProjectContext.Namespace;

        public ProjectContext ProjectContext { get; }
        public abstract string Name { get; }
        public string OriginalXml { get; }
        public int LineNumber { get; }
        public string CurrentXml => Element.ToString();

        internal ReferenceContextBase(ProjectContext context, XElement element)
        {
            ProjectContext = context;
            Element = element;
            OriginalXml = element.ToString();
            LineNumber = ((IXmlLineInfo) element).LineNumber;
            Include = Element.Element(ProjectContext.Namespace + IncludeString)?.Value ??
                      Element.Attribute(IncludeString)?.Value;
        }
    }
}
