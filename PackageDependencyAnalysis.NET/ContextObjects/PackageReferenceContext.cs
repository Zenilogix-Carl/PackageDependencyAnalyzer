using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.ContextObjects
{
    public class PackageReferenceContext : ReferenceContextBase
    {
        private const string VersionString = "Version";

        private static readonly Regex PackageFromHintPathRegex = new Regex(@"^(?<Root>.*)[/\\]packages[/\\](?<Package>.+\D)\.(?<Version>\d+(\.\d+){1,3})(-(?<Suffix>[^/\\]+)){0,1}[/\\](?<Leaf>.*)$");
        private static readonly Regex VersionRegex = new Regex(@"(?<Version>\d+(\.\d+){1,3})(-(?<Suffix>.+)){0,1}");
        private readonly XElement _versionElement;
        private readonly XAttribute _versionAttribute;
        private readonly string _root;
        private readonly string _leaf;
        private ReleaseVersion _version;

        internal ReferenceContext ReferenceContext { get; }

        internal string ReconstructedHintPath => $"{_root}\\packages\\{Name}.{Version}\\{_leaf}";

        public override string Name { get; }

        public ReleaseVersion Version
        {
            get => _version;
            set
            {
                _version = value;
                SetVersion();
            }
        }

        internal IList<PackageReferenceContext> AssemblyReferenceContexts { get; } = new List<PackageReferenceContext>();

        public IEnumerable<IAssemblyDetailsContext> AssemblyReferences => AssemblyReferenceContexts.Select(a => a.ReferenceContext);

        internal PackageReferenceContext(ProjectContext context, XElement element) : base(context, element)
        {
            Name = Include;
            _versionElement = element.Element(Namespace + VersionString);
            _versionAttribute = element.Attribute(VersionString);
            var versionStr = _versionElement?.Value ?? _versionAttribute?.Value;
            if (versionStr != null)
            {
                var match = VersionRegex.Match(versionStr);
                _version = new ReleaseVersion(new Version(match.Groups["Version"].Value)){Release = match.Groups["Suffix"]?.Value };
            }
        }

        internal PackageReferenceContext(ProjectContext context, ReferenceContext referenceContext) : base(context, referenceContext.Element)
        {
            ReferenceContext = referenceContext;
            Name = referenceContext.Name;
            var match = PackageFromHintPathRegex.Match(ReferenceContext.HintPathElement.Value);
            if (match.Success)
            {
                _root = match.Groups["Root"]?.Value;
                Name = match.Groups["Package"]?.Value;
                _version = new ReleaseVersion(new Version(match.Groups["Version"]?.Value ?? string.Empty)){Release = match.Groups["Suffix"]?.Value};
                _leaf = match.Groups["Leaf"]?.Value;
            }
        }

        internal static PackageReferenceContext FromReference(ReferenceContext context)
        {
            var packageReferenceContext = new PackageReferenceContext(context.ProjectContext, context);
            return packageReferenceContext.Version != null ? packageReferenceContext : null;
        }

        private void SetVersion()
        {
            if (_versionElement != null)
            {
                _versionElement.Value = _version.ToString();
            }
            else if (_versionAttribute != null)
            {
                _versionAttribute.Value = _version.ToString();
            }
            else
            {
                ReferenceContext.HintPathElement.Value = ReconstructedHintPath;
            }
        }
    }
}
