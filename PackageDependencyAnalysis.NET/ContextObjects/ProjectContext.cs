using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.ContextObjects
{
    public class ProjectContext
    {
        private const string ProjectString = "Project";
        private const string PropertyGroupString = "PropertyGroup";
        private const string ItemGroupString = "ItemGroup";
        private const string AssemblyNameString = "AssemblyName";
        private const string IncludeString = "Include";
        private const string OutputPathString = "OutputPath";
        private const string ReferenceString = "Reference";
        private const string PackageReferenceString = "PackageReference";
        private const string ProjectReferenceString = "ProjectReference";
        private const string VersionString = "Version";

        private static readonly Dictionary<ProjectItemType, string> ProjectItemTypesDictionary = new Dictionary<ProjectItemType, string>
        {
            { ProjectItemType.None, nameof(ProjectItemType.None) },
            { ProjectItemType.Compile, nameof(ProjectItemType.Compile) },
        };

        private readonly XElement _projectElement;
        private readonly XDocument _doc;
        internal XNamespace Namespace { get; }

        public SolutionContext SolutionContext { get; }
        public string FileName { get; }
        public string Directory => Path.GetDirectoryName(FileName);
        public string Name { get; }
        public Version Version { get; }
        public string PackagesConfigFileSpec { get; }
        public string AppConfigFileSpec { get; }

        public bool HasAppConfig => File.Exists(AppConfigFileSpec);
        public bool HasPackagesConfig => File.Exists(PackagesConfigFileSpec);

        internal ProjectContext(string fileSpec)
        {
            FileName = fileSpec;

            _doc = XDocument.Load(fileSpec, LoadOptions.PreserveWhitespace|LoadOptions.SetLineInfo);
            _projectElement = _doc.Root;

            Namespace = _projectElement?.Name.Namespace;
            var assemblyName = _projectElement?.Elements(Namespace + PropertyGroupString)
                .Select(s => s.Element(Namespace + AssemblyNameString)).FirstOrDefault()?.Value;
            Name = assemblyName ?? Path.GetFileNameWithoutExtension(fileSpec);
            var versionString = _projectElement?.Elements(Namespace + PropertyGroupString)
                .Select(s => s.Element(Namespace + VersionString)).FirstOrDefault()?.Value;
            PackagesConfigFileSpec = Path.GetFullPath(Path.Combine(Directory, "packages.config"));
            AppConfigFileSpec = Path.GetFullPath(Path.Combine(Directory, "app.config"));
            if (versionString != null && Version.TryParse(versionString, out var version))
            {
                Version = version;
            }
        }

        internal ProjectContext(SolutionContext solution, string fileSpec) : this(fileSpec)
        {
            SolutionContext = solution;
        }

        public static ProjectContext Create(string fileSpec) => new ProjectContext(fileSpec);

        internal static ProjectContext Create(SolutionContext solution, string fileSpec) => new ProjectContext(solution, fileSpec);

        /// <summary>
        /// Gets names of files matching specified criteria 
        /// </summary>
        /// <param name="types">Project item type(s)</param>
        /// <param name="matchString">Regex string</param>
        /// <returns></returns>
        public IEnumerable<string> GetSourceFiles(ProjectItemType types, string matchString)
        {
            var regex = new Regex(matchString);

            foreach (var xItemGroupElement in _projectElement.Elements(Namespace + ItemGroupString))
            {
                foreach (ProjectItemType type in Enum.GetValues(typeof(ProjectItemType)))
                {
                    if ((type & types) == type && type != ProjectItemType.All)
                    {
                        foreach (var elementOfType in xItemGroupElement.Elements(Namespace + ProjectItemTypesDictionary[type]))
                        {
                            var fileName = elementOfType.Attribute(IncludeString)?.Value;
                            if (fileName != null && (string.IsNullOrEmpty(matchString) || regex.IsMatch(fileName)))
                            {
                                yield return Path.GetFullPath(Path.Combine(Directory, fileName));
                            }
                        }
                    }
                }
            }
        }

        public IEnumerable<ProjectReferenceContext> ProjectReferences
        {
            get
            {
                foreach (var xItemGroupElement in _projectElement.Elements(Namespace + ItemGroupString))
                {
                    foreach (var referenceElement in xItemGroupElement.Elements(Namespace + ProjectReferenceString))
                    {
                        yield return new ProjectReferenceContext(this, referenceElement);
                    }
                }
            }
        }

        public IEnumerable<PackageReferenceContext> PackageReferences
        {
            get
            {
                foreach (var xItemGroupElement in _projectElement.Elements(Namespace + ItemGroupString))
                {
                    foreach (var referenceElement in xItemGroupElement.Elements(Namespace + PackageReferenceString))
                    {
                        yield return new PackageReferenceContext(this, referenceElement);
                    }
                }
            }
        }

        public IEnumerable<string> OutputPaths
        {
            get
            {
                foreach (var xItemGroupElement in _projectElement.Elements(Namespace + PropertyGroupString))
                {
                    foreach (var outputPathElement in xItemGroupElement.Elements(Namespace + OutputPathString))
                    {
                        yield return Path.GetFullPath(Path.Combine(Directory, outputPathElement.Value));
                    }
                }
            }
        }

        public PackagesConfigContext GetPackagesConfigContext() => HasPackagesConfig ? PackagesConfigContext.Create(PackagesConfigFileSpec) : null;

        public AppConfigContext GetAppConfigContext() => HasAppConfig ? AppConfigContext.Create(AppConfigFileSpec) : null;

        public IEnumerable<IProject> GetProjectReferences(Func<ProjectReferenceContext, IProject> projectLookupFunc) => ProjectReferences.Select(projectLookupFunc);
        public IEnumerable<IPackage> GetPackages(Func<PackageReferenceContext, IPackage> projectFactoryFunc) => PackageReferences.Select(projectFactoryFunc);

        public void Save()
        {
            _doc.Save(FileName);
        }

        public override string ToString() => FileName;

        internal IEnumerable<ReferenceContext> References
        {
            get
            {
                foreach (var xItemGroupElement in _projectElement.Elements(Namespace + ItemGroupString))
                {
                    foreach (var referenceElement in xItemGroupElement.Elements(Namespace + ReferenceString))
                    {
                        var context = ReferenceContext.Create(this, referenceElement);
                        if (context != null)
                        {
                            yield return context;
                        }
                    }
                }
            }
        }
    }
}
