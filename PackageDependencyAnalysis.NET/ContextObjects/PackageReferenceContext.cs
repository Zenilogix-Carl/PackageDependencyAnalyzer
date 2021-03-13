using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PackageDependencyAnalysis.ContextObjects
{
    public class PackageReferenceContext : ReferenceContextBase, IAssemblyReferenceContext
    {
        private const string VersionString = "Version";

        private static readonly Regex PackageFromHintPathRegex = new Regex(@"^(?<Root>.*)[/\\]packages[/\\](?<Package>.+\D)\.(?<Version>\d+(\.\d+){1,3})(-(?<Suffix>[^/\\]+)){0,1}[/\\](?<Leaf>.*)$");
        private static readonly Regex VersionRegex = new Regex(@"(?<Version>\d+(\.\d+){1,3})(-(?<Suffix>.+)){0,1}");
        private readonly XElement _versionElement;
        private readonly XAttribute _versionAttribute;
        private readonly string _root;
        private readonly string _leaf;
        private Version _version;
        private string _preRelease;

        internal ReferenceContext ReferenceContext { get; }

        internal string ReconstructedHintPath => IsPreRelease ? $"{_root}\\packages\\{Name}.{Version}-{PreRelease}\\{_leaf}" : $"{_root}\\packages\\{Name}.{Version}\\{_leaf}";

        public bool IsPreRelease => !string.IsNullOrWhiteSpace(_preRelease);

        public override string Name { get; }

        public Version Version
        {
            get => _version;
            set
            {
                _version = value;
                SetVersion();
            }
        }

        public string PreRelease
        {
            get => _preRelease;
            set
            {
                _preRelease = value;
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
                _version = new Version(match.Groups["Version"].Value);
                _preRelease = match.Groups["Suffix"]?.Value;
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
                _version = new Version(match.Groups["Version"]?.Value ?? string.Empty);
                _preRelease = match.Groups["Suffix"]?.Value;
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
                _versionElement.Value = IsPreRelease ? $"{_version}-{_preRelease}" : _version.ToString();
            }
            else if (_versionAttribute != null)
            {
                _versionAttribute.Value = IsPreRelease ? $"{_version}-{_preRelease}" : _version.ToString();
            }
            else
            {
                ReferenceContext.HintPathElement.Value = ReconstructedHintPath;
            }
        }
    }
}
