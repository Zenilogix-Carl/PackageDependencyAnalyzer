using System.IO;
using System.Xml.Linq;

namespace PackageDependencyAnalysis.ContextObjects
{
    public class ProjectReferenceContext : ReferenceContextBase
    {
        private const string NameString = "Name";

        public override string Name { get; }

        public string FileName => Path.GetFullPath(Path.Combine(ProjectContext.Directory, Include));

        internal ProjectReferenceContext(ProjectContext context, XElement element) : base(context, element)
        {
            Name = element.Element(Namespace + NameString)?.Value;
        }

        internal static ProjectReferenceContext FromReference(ReferenceContext context)
        {
            var reference = new ProjectReferenceContext(context.ProjectContext, context.Element);
            return reference.Name == null ? null : reference;
        }
    }
}
