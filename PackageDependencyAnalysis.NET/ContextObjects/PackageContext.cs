using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Net;
using ICSharpCode.SharpZipLib.Zip;

namespace PackageDependencyAnalysis.ContextObjects
{
    public class PackageContext
    {
        private readonly Func<string, bool> _frameworkFilter;

        public class AssemblyInfo
        {
            internal static Regex FrameworkRegex = new Regex(@"lib[\\/](?<Framework>.+)[\\/]{0,1}");

            public string FileName { get; set; }
            public ICollection<string> Folders { get; } = new List<string>();
            public IEnumerable<string> Frameworks => Folders.Select(Framework);

            internal static string Framework(string localPath)
            {
                var match = FrameworkRegex.Match(localPath);
                return match.Success ? match.Groups["Framework"]?.Value.ToLower() : null;
            }

            public override string ToString() => $"{FileName} ({Folders.Count} folders - {string.Join(", ", Folders)})";
        }

        private const string MetadataString = "metadata";
        private const string IdString = "id";
        private const string VersionString = "version";
        private const string RepositoryString = "repository";
        private const string DescriptionString = "description";
        private const string DependenciesString = "dependencies";
        private const string ReferencesString = "references";
        private const string GroupString = "group";
        private const string RepositoryUrlString = "url";

        private readonly PackageCacheContext _context;
        private XElement _dependenciesElement;
        private XElement _referencesElement;
        private static readonly Regex VersionRegex = new Regex(@"(?<Version>\d+(\.\d+){1,3})(-(?<Suffix>.+)){0,1}");
        public string RawNuSpec;
        private XNamespace _ns;
        private IDictionary<string, AssemblyInfo> _assemblies = new Dictionary<string, AssemblyInfo>();

        /// <summary>
        /// Full path of the nupkg file (if cached)
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Folder location containing the nupkg file (if cached)
        /// </summary>
        public string Directory { get; private set; }

        /// <summary>
        /// Nuget source where the package was found (if not cached)
        /// </summary>
        public string Source { get; set; }
        public string  Name { get; private set; }
        public DateTime DateTime { get; private set; }
        public string Description { get; private set; }
        public string RepositoryUrl { get; private set; }
        public Version Version { get; private set; }
        public string PreReleaseSuffix { get; private set; }
        public bool IsPreRelease => string.IsNullOrEmpty(PreReleaseSuffix);
        public ICollection<AssemblyInfo> Assemblies => _assemblies.Values;
        public IDictionary<string,ICollection<string>> Folders = new Dictionary<string, ICollection<string>>();

        public IEnumerable<NuSpecReference> Dependencies
        {
            get
            {
                if (_dependenciesElement != null)
                {
                    foreach (var nuSpecReference in new NuSpecReferencesContext(_dependenciesElement, _ns).References)
                    {
                        if (nuSpecReference.Name != null)
                        {
                            yield return nuSpecReference;
                        }
                    }
                }
                if (_referencesElement != null)
                {
                    foreach (var nuSpecReference in new NuSpecReferencesContext(_referencesElement, _ns).References)
                    {
                        if (nuSpecReference.Name != null)
                        {
                            yield return nuSpecReference;
                        }
                    }
                }
            }
        }

        public IEnumerable<NuSpecDependencyGroup> Groups
        {
            get
            {
                if (_dependenciesElement != null)
                {
                    foreach (var groupElement in _dependenciesElement.Elements(_ns + GroupString))
                    {
                        yield return new NuSpecDependencyGroup(groupElement, _ns);
                    }
                }
                if (_referencesElement != null)
                {
                    foreach (var groupElement in _referencesElement.Elements(_ns + GroupString))
                    {
                        yield return new NuSpecDependencyGroup(groupElement, _ns);
                    }
                }
            }
        }

        internal PackageContext(PackageCacheContext context, string nugetFileSpec, Func<string,bool> frameworkFilter=null) : this(nugetFileSpec, frameworkFilter)
        {
            _context = context;
        }

        internal PackageContext(string nugetFileSpec, Func<string,bool> frameworkFilter=null)
        {
            _frameworkFilter = frameworkFilter;
            ProcessNugetPackage(nugetFileSpec);
        }

        internal PackageContext(Stream stream, Func<string, bool> frameworkFilter = null)
        {
            _frameworkFilter = frameworkFilter;
            ProcessNugetPackage(stream);
        }

        public static PackageContext Create(string nugetFileSpec, Func<string, bool> frameworkFilter=null) => new PackageContext(nugetFileSpec, frameworkFilter);
        internal static PackageContext Create(PackageCacheContext context, string nugetFileSpec, Func<string, bool> frameworkFilter=null) => new PackageContext(context, nugetFileSpec, frameworkFilter);

        public void ProcessNugetPackage(string nugetFileSpec)
        {
            FileName = nugetFileSpec;

            Directory = Path.GetDirectoryName(nugetFileSpec);
            using (var stream = File.OpenRead(nugetFileSpec))
            {
                ProcessNugetPackage(stream);
            }
        }

        public void ProcessNugetPackage(Stream packageStream)
        {
            using (var archive = new ZipFile(packageStream))
            {
                foreach (ZipEntry entry in archive)
                {
                    if (entry.IsFile)
                    {
                        if (entry.Name.EndsWith(".nuspec", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var stream = archive.GetInputStream(entry);
                            DateTime = entry.DateTime;
                            using (var reader = new StreamReader(stream))
                            {
                                RawNuSpec = reader.ReadToEnd();
                            }

                            ProcessNuSpecXml();
                        }
                        else if (entry.Name.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var unencodedFullName = WebUtility.UrlDecode(entry.Name);
                            var unencodedName = Path.GetFileName(unencodedFullName);
                            var folderName = Path.GetDirectoryName(unencodedFullName) ?? ".";
                            var framework = AssemblyInfo.Framework(folderName);

                            if (_frameworkFilter == null ||
                                framework != null && _frameworkFilter(AssemblyInfo.Framework(folderName)))
                            {
                                if (!_assemblies.TryGetValue(unencodedName, out var assemblyInfo))
                                {
                                    assemblyInfo = new AssemblyInfo {FileName = unencodedName};
                                    _assemblies[unencodedName] = assemblyInfo;
                                }

                                assemblyInfo.Folders.Add(folderName);

                                if (!Folders.TryGetValue(folderName, out var folder))
                                {
                                    folder = new List<string>();
                                    Folders[folderName] = folder;
                                }

                                folder.Add(unencodedName);
                            }
                        }
                    }
                }
            }
        }

        private void ProcessNuSpecXml()
        {
            var doc = XDocument.Parse(RawNuSpec);
            _ns = doc.Root?.Name.Namespace;
            var metaDataElement = doc.Root?.Element(_ns + MetadataString);
            if (metaDataElement == null)
            {
                metaDataElement = doc.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == MetadataString);
                _ns = metaDataElement?.Name.Namespace;
            }

            Name = metaDataElement?.Element(_ns + IdString)?.Value;
            var versionString = metaDataElement?.Element(_ns + VersionString)?.Value ?? string.Empty;
            var repositoryElement = metaDataElement?.Element(_ns + RepositoryString);
            Description = metaDataElement?.Element(_ns + DescriptionString)?.Value ?? string.Empty;
            RepositoryUrl = repositoryElement?.Attribute(RepositoryUrlString)?.Value;

            var versionMatch = VersionRegex.Match(versionString);
            if (versionMatch.Success)
            {
                Version = new Version(versionMatch.Groups["Version"].Value);
                PreReleaseSuffix = versionMatch.Groups["Suffix"].Value;
            }

            _dependenciesElement = metaDataElement?.Element(_ns + DependenciesString);
            _referencesElement = metaDataElement?.Element(_ns + ReferencesString);
        }
    }
}
