using System;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.ContextObjects
{
    public class PackagesConfigPackageContext
    {
        private const string IdString = "id";
        private const string VersionString = "version";
        private const string TargetFrameworkString = "targetFramework";

        private static readonly Regex VersionRegex = new Regex(@"(?<Version>\d+(\.\d+){1,3})(-(?<Suffix>.+)){0,1}");

        private readonly XElement _element;
        private ReleaseVersion _version;
        private readonly XAttribute _versionAttribute;
        public PackagesConfigContext Context { get; }
        public string Id => _element.Attribute(IdString)?.Value;
        public string TargetFramework => _element.Element(TargetFrameworkString)?.Value;

        public string OriginalXml { get; }
        public int LineNumber { get; }
        public string CurrentXml => _element.ToString();

        public ReleaseVersion Version
        {
            get => _version;
            set
            {
                _version = value;
                SetVersion();
            }
        }

        internal PackagesConfigPackageContext(PackagesConfigContext context, XElement element)
        {
            OriginalXml = element.ToString();
            LineNumber = ((IXmlLineInfo)element).LineNumber;
            _element = element;
            Context = context;
            _versionAttribute = _element.Attribute(VersionString);
            var match = VersionRegex.Match(_versionAttribute?.Value ?? string.Empty);
            if (match.Success)
            {
                _version = new ReleaseVersion(new Version(match.Groups["Version"]?.Value ?? string.Empty)){Release = match.Groups["Suffix"]?.Value};
            }
        }

        private void SetVersion()
        {
            _versionAttribute.Value = _version.ToString();
        }
    }
}
