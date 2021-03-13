using System;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace PackageDependencyAnalysis.ContextObjects
{
    public class NuSpecReference
    {
        private const string IdString = "id";
        private const string VersionString = "version";

        private static readonly Regex VersionRegex = new Regex(@"(?<Version>\d+(\.\d+){1,3})(-(?<Suffix>.+)){0,1}");
        private static readonly Regex RangeVersionRegex = new Regex(@"(?<Prefix>(\[|\())((?<VersionFrom>\d+(\.\d+){1,3}){0,1}),((?<VersionTo>\d+(\.\d+){1,3}){0,1})(?<Suffix>(\]|\)))"); // see https://docs.microsoft.com/en-us/nuget/concepts/package-versioning

        private readonly XElement _nuSpecReferenceElement;

        public string Name => _nuSpecReferenceElement.Attribute(IdString)?.Value;
        public Version Version { get; }
        public string PreRelease { get; }
        public bool IsPreRelease => string.IsNullOrEmpty(PreRelease);

        public Version MinVersion { get; }
        public Version MaxVersion { get; }

        public bool MinExclusive { get; }
        public bool MaxExclusive { get; }

        internal static NuSpecReference Create(XElement nuSpecReferenceElement)
        {
            var nuSpecReference = new NuSpecReference(nuSpecReferenceElement);
            return nuSpecReference.Name == null || nuSpecReference.Version == null ? null : nuSpecReference;
        }

        internal NuSpecReference(XElement nuSpecReferenceElement)
        {
            _nuSpecReferenceElement = nuSpecReferenceElement;
            var versionString = nuSpecReferenceElement.Attribute(VersionString)?.Value;
            if (versionString != null)
            {
                var versionMatch = VersionRegex.Match(versionString);
                if (versionMatch.Success)
                {
                    Version = new Version(versionMatch.Groups["Version"].Value);
                    var preRelease = versionMatch.Groups["Suffix"]?.Value;
                    PreRelease = preRelease == string.Empty ? null : preRelease;
                }
                else
                {
                    versionMatch = RangeVersionRegex.Match(versionString);
                    if (versionMatch.Success)
                    {
                        MinExclusive = versionMatch.Groups["Prefix"].Value == "(";
                        MaxExclusive = versionMatch.Groups["Suffix"].Value == ")";
                        MinVersion = Version.TryParse(versionMatch.Groups["VersionFrom"]?.Value ?? string.Empty, out var minVersion) ? minVersion : null;
                        MaxVersion = Version.TryParse(versionMatch.Groups["VersionTo"]?.Value ?? string.Empty, out var maxVersion) ? maxVersion : null;
                    }
                }
            }
        }
    }
}
