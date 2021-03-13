using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using PackageDependencyAnalysis.ContextObjects;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Processors
{
    public class ProjectFileProcessor
    {
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

        public IProject Get(Dictionary<string,ProjectContext> dictionary, string path)
        {
            return Get(dictionary, path, 0);
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
            document = null;
            referenceObject = null;
            return null;
        }

        public static string ReplacePackageReference(object document, object referenceObject, PackageReference newReference)
        {
            return null;
        }

        public static void Save(IProject project, object document)
        {
        }

        private IProject Get(Dictionary<string, ProjectContext> dictionary, string path, int depth)
        {
            if (!_solution.ProjectCache.TryGetValue(path, out var project))
            {
                project = CreateFromFile(dictionary, path, depth);
                _solution.ProjectCache[path] = project;
            }

            return project;
        }

        private IProject CreateFromFile(Dictionary<string, ProjectContext> dictionary, string path, int depth)
        {
            var projectContext = dictionary[path];

            var project = _projectFactoryFunc(path);

            _logger.WriteLine($"{new string(' ', depth)}{path}");


            project.Name = projectContext.Name;
            project.Version = projectContext.Version;

            foreach (var packageReferenceContext in projectContext.PackageReferences)
            {
                var reference = CreatePackageReference(packageReferenceContext.Name, packageReferenceContext.Version);
                reference.PreReleaseSuffix = packageReferenceContext.PreRelease;
                if (project.PackageReferenceDictionary.TryGetValue(reference.Package.Name,
                    out var existingReference))
                {
                    _logger.Error(project,
                        $"There is already a reference to package {existingReference.Package.Name} at version {existingReference.Version}");
                }

                project.PackageReferenceDictionary[reference.Package.Name] = reference;
            }

            foreach (var projectReferenceContext in projectContext.ProjectReferences)
            {
                var subProject = Get(dictionary, projectReferenceContext.FileName, depth + 1);
                project.Projects.Add(subProject);
            }

            _appConfigFileProcessor.HandleAppConfig(project);
            _packagesConfigFileProcessor.HandlePackagesConfig(project);

            return project;
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
