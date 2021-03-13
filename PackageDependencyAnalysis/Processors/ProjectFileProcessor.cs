using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Processors
{
    public class ProjectFileProcessor
    {
        private const string ProjectString = "Project";
        private const string PropertyGroupString = "PropertyGroup";
        private const string ItemGroupString = "ItemGroup";
        private const string AssemblyNameString = "AssemblyName";
        private const string ReferenceString = "Reference";
        private const string PackageReferenceString = "PackageReference";
        private const string ProjectReferenceString = "ProjectReference";
        private const string IncludeString = "Include";
        private const string VersionString = "Version";
        private const string HintPathString = "HintPath";

        private static readonly Regex PackageFromHintPathRegex = new Regex(@"[/\\]packages[/\\](?<Package>.+\D)\.(?<Version>\d+(\.\d+){2,3})(-(?<Suffix>[^/\\]+)){0,1}[/\\]");

        private readonly Func<string,IProject> _projectFactoryFunc;
        private readonly Func<string, IPackage> _packageFactoryFunc;
        private readonly ISolution _solution;
        private readonly PackageCacheProcessor _packageCacheProcessor;
        private readonly AppConfigFileProcessor _appConfigFileProcessor;
        private readonly PackagesConfigFileProcessor _packagesConfigFileProcessor;
        private readonly IPackageCache _packageCache;
        private readonly ILogger _logger;

        public ProjectFileProcessor(Func<string,IProject> projectFactoryFunc, Func<string, IPackage> packageFactoryFunc, ISolution solution, PackageCacheProcessor packageCacheProcessor, AppConfigFileProcessor appConfigFileProcessor, PackagesConfigFileProcessor packagesConfigFileProcessor, IPackageCache packageCache, ILogger logger)
        {
            _projectFactoryFunc = projectFactoryFunc ?? throw new ArgumentNullException(nameof(projectFactoryFunc));
            _packageFactoryFunc = packageFactoryFunc ?? throw new ArgumentNullException(nameof(packageFactoryFunc));
            _solution = solution ?? throw new ArgumentNullException(nameof(solution));
            _packageCacheProcessor = packageCacheProcessor ?? throw new ArgumentNullException(nameof(packageCacheProcessor));
            _appConfigFileProcessor = appConfigFileProcessor ?? throw new ArgumentNullException(nameof(appConfigFileProcessor));
            _packagesConfigFileProcessor = packagesConfigFileProcessor;
            _packageCache = packageCache ?? throw new ArgumentNullException(nameof(packageCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (_packageCache.Packages == null) throw new ArgumentException($"{nameof(_packageCache.Packages)} was not initialized", nameof(packageCache));
            if (_packageCache.PackagesDictionary == null) throw new ArgumentException($"{nameof(_packageCache.PackagesDictionary)} was not initialized", nameof(packageCache));
        }

        public IProject Get(string path)
        {
            return Get(path, 0);
        }

        public void ResolvePackageReferences()
        {
            foreach (var project in _solution.ProjectCache.Values)
            {
                foreach (var packageReference in project.PackageReferences)
                {
                    packageReference.Package.ReferencingProjects[project.Name] = project;

                    if (packageReference.ResolvedReference == null)
                    {
                        if (_packageCacheProcessor.ResolvePackageReference(project, packageReference))
                        {
                            packageReference.ResolvedReference?.ReferencingProjects.Add(project);
                        }
                    }

                    if (packageReference.BindingRedirection != null)
                    {
                        _packageCacheProcessor.ResolveBindingRedirection(project, packageReference.BindingRedirection);
                    }

                    if (packageReference.PackagesConfigVersion != null)
                    {
                        _packageCacheProcessor.ResolvePackagesConfig(project, packageReference);
                    }
                }
            }
        }

        public static string GetPackageReference(IProject project, PackageReference reference, out object document, out object referenceObject)
        {
            var doc = XDocument.Load(project.AbsolutePath);
            var ns = doc.Root.Name.Namespace;

            referenceObject = doc.Root
                .Elements(ns + ItemGroupString)
                .SelectMany(g => g.Elements(ns + PackageReferenceString)).FirstOrDefault(e => e.Attribute(IncludeString)?.Value == reference.Package.Name);

            //if (referenceObject == null)
            //{
            //    referenceObject = doc.Root
            //        .Elements(ns + ItemGroupString)
            //        .SelectMany(g => g.Elements(ns + ReferenceString)).FirstOrDefault(e =>
            //        {
            //            var hintPath = e.Element(ns + HintPathString)?.Value;
            //            if (hintPath != null)
            //            {
            //                var match = PackageFromHintPathRegex.Match(hintPath);
            //                if (match.Success)
            //                {
            //                    var packageName = match.Groups["Package"].Value;
            //                    return packageName == reference.Package.Name;
            //                }
            //            }

            //            return false;
            //        });
            //}

            document = doc;
            return referenceObject?.ToString();
        }

        public static string ReplacePackageReference(object document, object referenceObject, PackageReference newReference)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (referenceObject == null) throw new ArgumentNullException(nameof(referenceObject));
            if (!(document is XDocument doc)) throw new ArgumentException(nameof(document));
            if (!(referenceObject is XElement element)) throw new ArgumentException(nameof(referenceObject));

            var include = element.Attribute(IncludeString);
            if (element.Name.LocalName == ReferenceString)
            {
                var parts = include?.Value.Split(',').Select(s => s.Trim()).ToArray();
                if (parts?.Length > 1)
                {
                    var dictionary = parts.Skip(1).Select(s => s.Split('=')).ToDictionary(s => s[0].Trim(), s => s[1].Trim());
                    dictionary[VersionString] = newReference.Version.ToString();
                    include.Value = $"{parts[0]}, {string.Join(", ", dictionary.Select(s => $"{s.Key}={s.Value}"))}";
                }

                var hintPathElement = element.Element(doc.Root.Name.Namespace+HintPathString);
                if (hintPathElement != null)
                {
                    hintPathElement.Value = newReference.HintPath;
                }
            }
            else if (element.Name.LocalName == PackageReferenceString)
            {
                var versionAttribute = element.Attribute(VersionString);
                var versionElement = element.Element(VersionString);

                var newVersionString = string.IsNullOrWhiteSpace(newReference.PreReleaseSuffix)
                    ? newReference.Version.ToString()
                    : $"{newReference.Version}-{newReference.PreReleaseSuffix}";

                if (versionElement != null)
                {
                    versionElement.Value = newVersionString;
                }
                else if (versionAttribute != null)
                {
                    versionAttribute.Value = newVersionString;
                }
            }

            return element.ToString();
        }

        public static void Save(IProject project, object document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (!(document is XDocument doc)) throw new ArgumentException(nameof(document));
            doc.Save(project.AbsolutePath);
        }

        private IProject Get(string path, int depth)
        {
            if (!_solution.ProjectCache.TryGetValue(path, out var project))
            {
                project = CreateFromFile(path, depth);
                _solution.ProjectCache[path] = project;
            }

            return project;
        }

        private IProject CreateFromFile(string path, int depth)
        {
            var project = _projectFactoryFunc(path);
            var localPath = Path.GetDirectoryName(path);

            _logger.WriteLine($"{new string(' ', depth)}{path}");

            var doc = XDocument.Load(path);
            var nameSpace = doc.Root.Name.Namespace;

            var projectElement = doc.Root;

            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            var assemblyName = projectElement?.Elements(nameSpace + PropertyGroupString)
                .Select(s => s.Element(nameSpace + AssemblyNameString)).FirstOrDefault()?.Value;
            project.Name = assemblyName ?? fileNameWithoutExtension;
            var versionString = projectElement?.Elements(nameSpace + PropertyGroupString)
                .Select(s => s.Element(nameSpace + VersionString)).FirstOrDefault()?.Value;
            if (Version.TryParse(versionString, out var version))
            {
                project.Version = version;
            }

            foreach (var xItemGroupElement in projectElement?.Elements(nameSpace + ItemGroupString))
            {
                foreach (var xReferenceElement in xItemGroupElement.Elements(nameSpace + ReferenceString))
                {
                    if (ExtractPackageReference(xReferenceElement, out var reference))
                    {
                        if (project.PackageReferenceDictionary.TryGetValue(reference.Package.Name, out var existingReference))
                        {
                            _logger.Error(project, $"There is already a reference to package {existingReference.Package.Name} at version {existingReference.Version}");
                        }
                        project.PackageReferenceDictionary[reference.Package.Name] = reference;
                    }
                    else
                    {
                        
                    }
                }

                foreach (var xReferenceElement in xItemGroupElement.Elements(nameSpace + PackageReferenceString))
                {
                    if (ExtractPackageReference(xReferenceElement, out var reference))
                    {
                        if (project.PackageReferenceDictionary.TryGetValue(reference.Package.Name, out var existingReference))
                        {
                            _logger.Error(project, $"There is already a reference to package {existingReference.Package.Name} at version {existingReference.Version}");
                        }
                        project.PackageReferenceDictionary[reference.Package.Name] = reference;
                    }
                }

                foreach (var xReferenceElement in xItemGroupElement.Elements(nameSpace + ProjectReferenceString))
                {
                    if (ExtractProjectReference(project, xReferenceElement, localPath, depth, out var subProject))
                    {
                        project.Projects.Add(subProject);
                    }
                }
            }

            _appConfigFileProcessor.HandleAppConfig(project);
            _packagesConfigFileProcessor.HandlePackagesConfig(project);

            return project;
        }

        private bool ExtractPackageReference(XElement element, out PackageReference reference)
        {
            if (element.Name.LocalName == ReferenceString)
            {
                var hintPath = element.Element(element.Name.Namespace + HintPathString)?.Value;
                if (hintPath != null)
                {
                    var match = PackageFromHintPathRegex.Match(hintPath);
                    if (match.Success)
                    {
                        var packageName = match.Groups["Package"].Value;
                        var version = new Version(match.Groups["Version"].Value);
                        var suffix = match.Groups["Suffix"]?.Value;
                        reference = CreatePackageReference(packageName, version);
                        if (suffix != null)
                        {
                            reference.PreReleaseSuffix = suffix;
                        }
                        reference.HintPath = hintPath;
                        return true;
                    }
                }
            }
            else if (element.Name.LocalName == PackageReferenceString)
            {
                var packageName = element.Attribute(IncludeString)?.Value;
                var versionAttribute = element.Attribute(VersionString);
                var versionElement = element.Element(VersionString);

                var versionString = versionElement?.Value ?? versionAttribute?.Value;
                if (Version.TryParse(versionString ?? string.Empty, out var version))
                {
                    reference = CreatePackageReference(packageName, version);
                    return true;
                }
            }

            reference = null;
            return false;
        }

        private bool ExtractProjectReference(IContext context, XElement element, string localPath, int depth, out IProject project)
        {
            var include = element.Attribute(IncludeString);

            var path = Path.Combine(localPath, include?.Value ?? string.Empty);
            path = Path.GetFullPath(path);

            if (File.Exists(path))
            {
                project = Get(path, depth + 1);
                return true;
            }
            else
            {
                _logger.Warning(context, $"{context.File} references missing project {path}");
            }

            project = null;
            return false;
        }

        private PackageReference CreatePackageReference(string packageName, Version version)
        {
            if (!_packageCache.PackagesDictionary.TryGetValue(packageName, out var package))
            {
                package = _packageFactoryFunc(packageName);
                _packageCache.PackagesDictionary[packageName] = package;
            }

            return new PackageReference
            {
                Package = package,
                Version = version
            };
        }
    }
}
