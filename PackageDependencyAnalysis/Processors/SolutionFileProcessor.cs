using System;
using PackageDependencyAnalysis.ContextObjects;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Processors
{
    public class SolutionFileProcessor
    {
        private readonly ISolution _solution;
        private readonly IPackageCache _packageCache;
        private readonly Func<string, IProject> _projectFactoryFunc;
        private readonly Func<string, IPackage> _packageFactoryFunc;
        private readonly PackageCacheProcessor _packageCacheProcessor;
        private readonly ILogger _logger;

        public SolutionFileProcessor(ISolution solution, IPackageCache packageCache, Func<string, IProject> projectFactoryFunc, Func<string, IPackage> packageFactoryFunc, PackageCacheProcessor packageCacheProcessor, ILogger logger)
        {
            _solution = solution ?? throw new ArgumentNullException(nameof(solution));
            _packageCache = packageCache ?? throw new ArgumentNullException(nameof(packageCache));
            _projectFactoryFunc = projectFactoryFunc ?? throw new ArgumentNullException(nameof(projectFactoryFunc));
            _packageFactoryFunc = packageFactoryFunc ?? throw new ArgumentNullException(nameof(packageFactoryFunc));
            _packageCacheProcessor = packageCacheProcessor ?? throw new ArgumentNullException(nameof(packageCacheProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (_solution.Projects == null)
                throw new ArgumentException("Projects collection must be initialized", nameof(solution));
        }

        public void LoadFromFile(string path)
        {
            var solution = SolutionContext.Create(path);

            _solution.ProjectCache.Clear();

            foreach (var solutionProject in solution.Projects)
            {
                GetProject(solution, solutionProject.FileName);
            }

            _solution.SetFile(path);
        }

        private IProject GetProject(SolutionContext context, string name)
        {
            if (!_solution.ProjectCache.TryGetValue(name, out var project))
            {
                var projectContext = context.GetProject(name);

                project = _projectFactoryFunc(projectContext.FileName);
                project.Name = projectContext.Name;
                project.Version = projectContext.Version;
                _solution.ProjectCache[projectContext.FileName] = project;

                foreach (var packageReferenceContext in projectContext.PackageReferences)
                {
                    if (!_packageCache.PackagesDictionary.TryGetValue(packageReferenceContext.Name, out var package))
                    {
                        package = _packageFactoryFunc(packageReferenceContext.Name);
                        _packageCache.PackagesDictionary[packageReferenceContext.Name] = package;
                    }

                    var reference = new PackageReference
                    {
                        Package = package,
                        Version = packageReferenceContext.Version,
                        PreReleaseSuffix = packageReferenceContext.PreRelease
                    };

                    if (project.PackageReferenceDictionary.TryGetValue(reference.Package.Name, out var existingReference))
                    {
                        _logger.Error(project,
                            $"There is already a reference to package {existingReference.Package.Name} at version {existingReference.Version}");
                    }

                    project.PackageReferenceDictionary[reference.Package.Name] = reference;
                }

                foreach (var projectReferenceContext in projectContext.ProjectReferences)
                {
                    if (!_solution.ProjectCache.TryGetValue(projectReferenceContext.FileName, out var subProject))
                    {
                        subProject = GetProject(context, projectReferenceContext.FileName);
                    }

                    project.Projects.Add(subProject);
                    subProject.Dependencies.Add(project);
                }
            }

            return project;
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
    }
}
