using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PackageDependencyAnalysis.Model;
using ICSharpCode.SharpZipLib.Zip;

namespace PackageDependencyAnalysis.Processors
{
    public class PackageCacheProcessor
    {
        private static readonly Regex VersionRegex = new Regex(@"(?<Version>\d+(\.\d+){1,3})(-(?<Suffix>.+)){0,1}");

        private readonly Func<string, IPackage> _packageFactoryFunc;
        private readonly Func<string, IPackageVersion> _packageVersionFactoryFunc;
        private readonly IPackageCache _packageCache;
        private readonly ILogger _logger;
        //private readonly Regex _simpleVersionRegex = new Regex(@"(?<Version>([0-9\.]+))");
        //private readonly Regex _preReleaseVersionRegex = new Regex(@"(([0-9\.]+)-(?<Suffix>[\w\.-]+))");
        private readonly Regex _rangeVersionRegex = new Regex(@"(\[|\()(([0-9\.,]+))(\]|\))"); // see https://docs.microsoft.com/en-us/nuget/concepts/package-versioning

        public PackageCacheProcessor(Func<string,IPackage> packageFactoryFunc,Func<string,IPackageVersion> packageVersionFactoryFunc, IPackageCache packageCache, ILogger logger)
        {
            _packageFactoryFunc = packageFactoryFunc ?? throw new ArgumentNullException(nameof(packageFactoryFunc));
            _packageVersionFactoryFunc = packageVersionFactoryFunc ?? throw new ArgumentNullException(nameof(packageVersionFactoryFunc));
            _packageCache = packageCache ?? throw new ArgumentNullException(nameof(packageCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (_packageCache.Packages == null) throw new ArgumentException($"{nameof(_packageCache.Packages)} was not initialized", nameof(packageCache));
            if (_packageCache.PackagesDictionary == null) throw new ArgumentException($"{nameof(_packageCache.PackagesDictionary)} was not initialized", nameof(packageCache));
        }

        /// <summary>
        /// Scans package cache folders and builds dependencies
        /// </summary>
        public void ProcessCaches(string solutionFile)
        {
            _packageCache.Clear();
            ProcessFolder(Path.Combine(Path.GetDirectoryName(solutionFile), "packages"));
            ProcessFolder(Path.Combine(Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%"), @".nuget\packages"));
            ResolvePackageReferences();
        }

        private void ResolvePackageReferences()
        {
            foreach (var packageCachePackage in _packageCache.Packages)
            {
                foreach (var packageVersion in packageCachePackage.PackageVersions)
                {
                    if (packageVersion != null)
                    {
                        foreach (var dependency in packageVersion.Dependencies.Values)
                        {
                            foreach (var packageReference in dependency.Values.Where(p => p.ResolvedReference == null))
                            {
                                packageReference.Package.ReferencingPackages[packageVersion.Package.Name] = packageVersion;

                                if (ResolvePackageReference(packageVersion, packageReference))
                                {
                                    packageReference.ResolvedReference.ReferencingPackages.Add(packageVersion);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resolves a package reference
        /// </summary>
        /// <param name="context"></param>
        /// <param name="packageReference"></param>
        public bool ResolvePackageReference(IContext context, PackageReference packageReference)
        {
            var package = packageReference.Package;

            if (package.VersionDictionary.TryGetValue(packageReference.Version, out var packageVersion))
            {
                packageReference.ResolvedReference = packageVersion;
                packageReference.PreReleaseSuffix = packageVersion.PreReleaseSuffix;
                return true;
            }
            else
            {
                var match = package.Versions.SingleOrDefault(v => v.Matches(packageReference.Version));
                if (match != null)
                {
                    packageReference.ResolvedReference = package.VersionDictionary[match];
                    packageReference.PreReleaseSuffix = packageReference.ResolvedReference.PreReleaseSuffix;
                    return true;
                }
            }

            _logger.Warning(context,
                $"Could not resolve package reference {packageReference.Package.Name} {packageReference.Version}; available versions: {string.Join(",", package.Versions)}");
            return false;
        }

        public void ResolveBindingRedirection(IProject project, BindingRedirection bindingRedirection)
        {
            if (_packageCache.PackagesDictionary.TryGetValue(bindingRedirection.AssemblyName, out var package))
            {
                if (package.VersionDictionary.TryGetValue(bindingRedirection.NewVersion, out var packageVersion))
                {
                    packageVersion.BindingRedirectReferences.Add(project);
                }
                else
                {
                    var match = package.Versions.SingleOrDefault(v => v.Matches(bindingRedirection.NewVersion));
                    if (match != null)
                    {
                        package.VersionDictionary[match].BindingRedirectReferences.Add(project);
                    }
                }
            }
        }

        public void ResolvePackagesConfig(IProject project, PackageReference packageReference)
        {
            if (packageReference.Package.VersionDictionary.TryGetValue(packageReference.PackagesConfigVersion, out var packageVersion))
            {
                if (packageReference.ResolvedReference == null)
                {
                    packageReference.ResolvedReference = packageVersion;
                }
                packageVersion.ConfigReferences.Add(project);
            }
        }

        public IEnumerable<string> ScanForIssues()
        {
            foreach (var package in _packageCache.Packages)
            {
                if (package.PackageVersions.Count(v => v.IsReferenced) > 1)
                {
                    yield return
                        $"{package.Name}: multiple versions in use: {string.Join(", ", package.PackageVersions.Where(v => v.IsReferenced).Select(v => v.Version.ToString()))}";
                }
            }
        }

        public ICollection<IPackage> GetReferencedPackages()
        {
            foreach (var package in _packageCache.Packages)
            {
                package.IsReferenced = false;
                foreach (var packageVersion in package.PackageVersions)
                {
                    packageVersion.IsReferenced = false;
                }
            }

            int i = 0;
            foreach (var package in _packageCache.Packages.Where(p => p.ReferencingProjects.Any()))
            {
                i++;
                package.IsReferenced = package.HasVersions;
                MarkReferences(package);
            }

            return _packageCache.Packages.Where(p => p.IsReferenced).ToList();
        }

        public static ICollection<IPackage> GetDependentPackages(IPackage package)
        {
            var packageDictionary = new Dictionary<string, IPackage>();

            GetDependentPackages(packageDictionary, package);

            return packageDictionary.Values;
        }

        private static void GetDependentPackages(Dictionary<string, IPackage> packageDictionary, IPackage package)
        {
            foreach (var referencingPackage in package.ReferencingPackages)
            {
                if (!packageDictionary.ContainsKey(referencingPackage.Key))
                {
                    packageDictionary[referencingPackage.Key] = referencingPackage.Value.Package;
                    GetDependentPackages(packageDictionary, referencingPackage.Value.Package);
                }
            }
        }

        public static ICollection<IPackageVersion> GetDependentPackages(IPackageVersion package)
        {
            var packageDictionary = new Dictionary<string, IPackageVersion>();

            GetDependentPackages(packageDictionary, package);

            return packageDictionary.Values;
        }

        private static void GetDependentPackages(Dictionary<string, IPackageVersion> packageDictionary, IPackageVersion package)
        {
            foreach (var referencingPackage in package.ReferencingPackages)
            {
                if (!packageDictionary.ContainsKey(referencingPackage.Package.Name))
                {
                    packageDictionary[referencingPackage.Package.Name] = referencingPackage;
                    GetDependentPackages(packageDictionary, referencingPackage);
                }
            }
        }

        private void MarkReferences(IPackage package)
        {
            //if (package.IsReferenced && package.HasVersions)
            //{
            //    return;
            //}

            foreach (var packageVersion in package.PackageVersions)
            {
                if (packageVersion.ReferencingProjects.Any())
                {
                    packageVersion.IsReferenced = true;
                }

                foreach (var platform in packageVersion.Dependencies.Values)
                {
                    foreach (var dependency in platform.Values)
                    {
                        if (!dependency.Visited)
                        {
                            dependency.Visited = true;
                            dependency.Package.IsReferenced = dependency.Package.HasVersions;
                            if (dependency.ResolvedReference != null)
                            {
                                dependency.ResolvedReference.IsReferenced = true;
                            }
                            MarkReferences(dependency.Package);
                        }
                    }
                }
            }
        }

        private void ProcessFolder(string path)
        {
            if (Directory.Exists(path))
            {
                foreach (var enumerateFile in Directory.EnumerateFiles(path, "*.nupkg", SearchOption.AllDirectories))
                {
                    ProcessNuspecFile(enumerateFile);
                }
            }
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
            if (metaDataElement == null)
            {
                metaDataElement = doc.Root?.Elements().FirstOrDefault(e => e.Name.LocalName == "metadata");
                ns = metaDataElement?.Name.Namespace;
            }
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

        private PackageReference CreatePackageReference(string name, Version version)
        {
            if (!_packageCache.PackagesDictionary.TryGetValue(name, out var package))
            {
                package = _packageFactoryFunc(name);

                _packageCache.PackagesDictionary[name] = package;
            }

            return new PackageReference { Package = package, Version = version};
        }

        private bool GetVersion(string versionString, out Version version, out bool isPrerelease, out string preRelease, out bool isRangeVersion)
        {
            var match = VersionRegex.Match(versionString);
            if (match.Success)
            {
                version = Version.Parse(match.Groups["Version"].Value);
                preRelease = match.Groups["Suffix"]?.Value;
                isPrerelease = !string.IsNullOrEmpty(preRelease);
                isRangeVersion = _rangeVersionRegex.IsMatch(versionString);
                return match.Success;
            }

            version = null;
            preRelease = null;
            isPrerelease = false;
            isRangeVersion = false;

            return false;
        }
    }
}
