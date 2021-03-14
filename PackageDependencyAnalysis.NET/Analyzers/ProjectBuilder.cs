using System;
using System.Collections.Generic;
using System.Linq;
using PackageDependencyAnalysis.ContextObjects;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Analyzers
{
    public class ProjectBuilder
    {
        private readonly ISolution _solution;
        private readonly IPackageCache _packageCache;
        private readonly Action<IProject,string> _logErrorAction;

        public ProjectBuilder(ISolution solution, IPackageCache packageCache, Action<IProject,string> logErrorAction)
        {
            _solution = solution;
            _packageCache = packageCache;
            _logErrorAction = logErrorAction;
        }

        /// <summary>
        /// Populates specified project object from context
        /// </summary>
        /// <param name="project"></param>
        /// <param name="context"></param>
        public IProject Build(IProject project, ProjectContext context)
        {
            project.AbsolutePath = context.FileName;
            project.Name = context.Name;
            project.Version = context.Version;
            project.PackagesConfigPath = context.PackagesConfigFileSpec;
            project.AppConfigPath = context.AppConfigFileSpec;

            foreach (var packageReferenceContext in context.PackageReferences)
            {
                if (_packageCache.PackagesDictionary.TryGetValue(packageReferenceContext.Name, out var package))
                {
                    var reference = new PackageReference
                    {
                        Package = package,
                        Version = packageReferenceContext.Version,
                        LineNumber = packageReferenceContext.LineNumber,
                        OriginalXml = packageReferenceContext.OriginalXml
                    };

                    if (project.PackageReferenceDictionary.TryGetValue(reference.Package.Name, out var existingReference))
                    {
                        _logErrorAction(project,
                            $"There is already a reference to package {existingReference.Package.Name} at version {existingReference.Version}");
                    }

                    project.PackageReferenceDictionary[reference.Package.Name] = reference;
                }
                else
                {
                    _logErrorAction(project, $"Package {packageReferenceContext.Name} not found in cache");
                }
            }

            if (context.HasPackagesConfig)
            {
                foreach (var packageContext in context.GetPackagesConfigContext().Packages)
                {
                    if (_packageCache.PackagesDictionary.TryGetValue(packageContext.Id, out var package))
                    {
                        if (!project.PackageReferenceDictionary.ContainsKey(package.Name))
                        {
                            project.PackageReferenceDictionary[package.Name] = new PackageReference
                            {
                                Package = package,
                                Version = packageContext.Version,
                                PackagesConfigLineNumber = packageContext.LineNumber
                            };
                        }
                    }
                    else
                    {
                        _logErrorAction(project, $"Package {packageContext.Id} not found in cache");
                    }
                }
            }

            foreach (var referenceContext in context.References.Where(c => c.IsPackage))
            {
                if (_packageCache.PackagesDictionary.TryGetValue(referenceContext.Name, out var package))
                {
                    if (!project.PackageReferenceDictionary.TryGetValue(package.Name, out var reference))
                    {
                        reference = new PackageReference
                        {
                            Package = package,
                            Version = referenceContext.AssemblyVersion,
                        };

                        project.PackageReferenceDictionary[package.Name] = reference;
                    }

                    if (reference.AssemblyReferences == null)
                    {
                        reference.AssemblyReferences = new List<AssemblyReference>();
                    }

                    reference.AssemblyReferences.Add(new AssemblyReference
                    {
                        Package = package,
                        HintPath = referenceContext.HintPath,
                        Version = referenceContext.AssemblyVersion
                    });
                }
                else
                {
                    _logErrorAction(project, $"Package {referenceContext.Name} not found in cache");
                }
            }

            if (context.HasAppConfig)
            {
                foreach (var assemblyRedirect in context.GetAppConfigContext().AssemblyRedirects)
                {
                    project.BindingRedirections.Add(new BindingRedirection
                    {
                        AssemblyName = assemblyRedirect.AssemblyName,
                        OldVersionFrom = assemblyRedirect.OldVersionFrom,
                        OldVersionTo = assemblyRedirect.OldVersionTo,
                        NewVersion = assemblyRedirect.NewVersion,
                        LineNumber = assemblyRedirect.LineNumber
                    });
                }
            }

            return project;
        }

        public void AddProjectReferences(SolutionContext solutionContext)
        {
            foreach (var projectContext in solutionContext.Projects)
            {
                var project = _solution.ProjectCache[projectContext.FileName];

                foreach (var subProjectContext in projectContext.ProjectReferences)
                {
                    project.Projects.Add(_solution.ProjectCache[subProjectContext.FileName]);
                }
            }
        }
    }
}
