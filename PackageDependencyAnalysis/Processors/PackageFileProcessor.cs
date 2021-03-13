using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ICSharpCode.SharpZipLib.Zip;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Processors
{
    public class PackageFileProcessor
    {
        private readonly Func<string, IPackage> _packageFactoryFunc;
        private readonly Func<string, IPackageVersion> _packageVersionFactoryFunc;
        private readonly IPackageCache _packageCache;
        private readonly ILogger _logger;
        private readonly Regex _simpleVersionRegex = new Regex(@"(?<Version>([0-9\.]+))");
        private readonly Regex _preReleaseVersionRegex = new Regex(@"(([0-9\.]+)-(?<Suffix>[\w\.-]+))");
        private readonly Regex _rangeVersionRegex = new Regex(@"(\[|\()(([0-9\.,]+))(\]|\))"); // see https://docs.microsoft.com/en-us/nuget/concepts/package-versioning

        public PackageFileProcessor(Func<string, IPackage> packageFactoryFunc, Func<string, IPackageVersion> packageVersionFactoryFunc, IPackageCache packageCache, ILogger logger)
        {
            _packageFactoryFunc = packageFactoryFunc ?? throw new ArgumentNullException(nameof(packageFactoryFunc));
            _packageVersionFactoryFunc = packageVersionFactoryFunc ?? throw new ArgumentNullException(nameof(packageVersionFactoryFunc));
            _packageCache = packageCache ?? throw new ArgumentNullException(nameof(packageCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (_packageCache.Packages == null) throw new ArgumentException($"{nameof(_packageCache.Packages)} was not initialized", nameof(packageCache));
            if (_packageCache.PackagesDictionary == null) throw new ArgumentException($"{nameof(_packageCache.PackagesDictionary)} was not initialized", nameof(packageCache));
        }

        private void ProcessNuspecFile(string path)
        {
            var zip = new ZipFile(path);
            foreach (ZipEntry entry in zip)
            {
                if (entry.IsFile && entry.Name.EndsWith(".nuspec", StringComparison.InvariantCultureIgnoreCase))
                {
                    var stream = zip.GetInputStream(entry);
                    var packageVersion = _packageVersionFactoryFunc(path);
                    packageVersion.DateTime = entry.DateTime;
                    ProcessNuspecXml(stream, packageVersion);
                }
            }
            zip.Close();
        }

        private void ProcessNuspecXml(Stream xmlStream, IPackageVersion packageVersion)
        {
            var reader = new StreamReader(xmlStream);
            var nuspec = reader.ReadToEnd();
            var doc = XDocument.Parse(nuspec);
            var ns = doc.Root?.Name.Namespace;
            var metaDataElement = doc.Root?.Element(ns + "metadata");
            var packageName = metaDataElement?.Element(ns + "id")?.Value;
            var versionString = metaDataElement?.Element(ns + "version")?.Value ?? string.Empty;
            var repositoryElement = metaDataElement?.Element(ns + "repository");
            packageVersion.Description = metaDataElement?.Element(ns + "description")?.Value ?? string.Empty;
            packageVersion.NuSpec = nuspec;
            packageVersion.RepositoryUrl = repositoryElement?.Attribute("url")?.Value;

            if (GetVersion(versionString, out var version, out var isPrerelease, out var preRelease, out _))
            {
                packageVersion.Version = version;
                packageVersion.IsPrerelease = isPrerelease;
                packageVersion.PreReleaseSuffix = preRelease;
                if (!_packageCache.PackagesDictionary.TryGetValue(packageName, out var package))
                {
                    package = _packageFactoryFunc(packageName);
                    _packageCache.PackagesDictionary[packageName] = package;
                }

                packageVersion.Package = package;
                package.VersionDictionary[version] = packageVersion;

                var dependenciesElement = metaDataElement?.Element(ns + "dependencies");
                if (dependenciesElement != null)
                {
                    HandleNuspecDependencies(packageVersion, ns, dependenciesElement, string.Empty);
                }

                var referencesElement = metaDataElement?.Element(ns + "references");
                if (referencesElement != null)
                {
                    HandleNuspecReferences(packageVersion, ns, referencesElement, string.Empty);
                }
            }
            else
            {
                _logger.Error(packageVersion, $"Package {packageName} version {versionString} not parseable");
            }
        }

        private void HandleNuspecDependencies(IPackageVersion packageVersion, XNamespace ns, XElement dependenciesElement, string framework)
        {
            foreach (var groupElement in dependenciesElement.Elements(ns + "group"))
            {
                var targetFramework = groupElement.Attribute("targetFramework")?.Value;
                HandleNuspecDependencies(packageVersion, ns, groupElement, targetFramework ?? string.Empty);
            }

            foreach (var dependencyElement in dependenciesElement.Elements(ns + "dependency"))
            {
                var name = dependencyElement.Attribute("id")?.Value;
                var versionString = dependencyElement.Attribute("version")?.Value ?? string.Empty;

                if (GetVersion(versionString, out var version, out var isPrerelease, out var preReleaseString, out var isRangeVersion))
                {
                    if (!packageVersion.Dependencies.TryGetValue(framework, out var frameworkDependencies))
                    {
                        frameworkDependencies = new Dependencies();
                        packageVersion.Dependencies[framework] = frameworkDependencies;
                    }

                    if (!frameworkDependencies.TryGetValue(name, out var packageReference))
                    {
                        packageReference = CreatePackageReference(name, version);
                        packageReference.IsPrerelease = isPrerelease;
                        packageReference.PreReleaseSuffix = preReleaseString;
                        packageReference.IsRange = isRangeVersion;
                        frameworkDependencies[name] = packageReference;
                    }
                }
                else
                {
                    _logger.Error(packageVersion, $"Package {name} version {versionString} not parseable");
                }
            }
        }

        private void HandleNuspecReferences(IPackageVersion packageVersion, XNamespace ns, XElement referencesElement, string framework)
        {
            foreach (var groupElement in referencesElement.Elements(ns + "group"))
            {
                var targetFramework = groupElement.Attribute("targetFramework")?.Value;
                HandleNuspecReferences(packageVersion, ns, groupElement, targetFramework ?? string.Empty);
            }

            foreach (var referenceElement in referencesElement.Elements(ns + "reference"))
            {
                var name = referenceElement.Attribute("id")?.Value;
                var versionString = referenceElement.Attribute("version")?.Value ?? string.Empty;
                if (name == null || string.IsNullOrEmpty(versionString))
                {
                    continue;
                }

                if (GetVersion(versionString, out var version, out var isPrerelease, out var preRelease, out var isRangeVersion))
                {
                    if (!packageVersion.Dependencies.TryGetValue(name, out var frameworkDependencies))
                    {
                        frameworkDependencies = new Dependencies();
                        packageVersion.Dependencies[name] = frameworkDependencies;
                    }

                    if (!frameworkDependencies.TryGetValue(framework, out var packageReference))
                    {
                        packageReference = CreatePackageReference(name, version);
                        packageReference.IsPrerelease = isPrerelease;
                        packageReference.PreReleaseSuffix = preRelease;
                        packageReference.IsRange = isRangeVersion;
                        frameworkDependencies[framework] = packageReference;
                    }
                }
                else
                {
                    _logger.Error(packageVersion, $"Package {name} version {versionString} not parseable");
                }
            }
        }

        private bool GetVersion(string versionString, out Version version, out bool isPrerelease, out string preRelease, out bool isRangeVersion)
        {
            var match = _simpleVersionRegex.Match(versionString);
            var bareVersionString = match.Groups["Version"].Value;
            isRangeVersion = _rangeVersionRegex.IsMatch(versionString);
            var preReleaseMatch = _preReleaseVersionRegex.Match(versionString);
            isPrerelease = preReleaseMatch.Success;
            preRelease = isPrerelease ? preReleaseMatch.Groups["Suffix"]?.Value : null;

            return Version.TryParse(bareVersionString, out version);
        }
    }
}
