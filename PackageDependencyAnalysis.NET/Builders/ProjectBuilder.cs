using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PackageDependencyAnalysis.ContextObjects;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalysis.Builders
{
    public class ProjectBuilder
    {
        private readonly Func<string, IProject> _projectFactoryFunc;
        private readonly PackageVersionBuilder _packageVersionBuilder;

        public ProjectBuilder(Func<string,IProject> projectFactoryFunc, PackageVersionBuilder packageVersionBuilder)
        {
            _projectFactoryFunc = projectFactoryFunc ?? throw new ArgumentNullException(nameof(projectFactoryFunc));
            _packageVersionBuilder = packageVersionBuilder ?? throw new ArgumentNullException(nameof(packageVersionBuilder));
        }

        public async Task<IProject> CreateProject(ProjectContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var project = _projectFactoryFunc(context.FileName);
            project.Name = context.Name;

            project.AbsolutePath = context.FileName;
            project.Name = context.Name;
            project.Version = context.Version;
            project.PackagesConfigPath = context.PackagesConfigFileSpec;
            project.AppConfigPath = context.AppConfigFileSpec;

            var tasks = new List<Task>();

            foreach (var packageReferenceContext in context.PackageReferences)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var packageVersion = await _packageVersionBuilder.GetOrCreatePackageVersion(packageReferenceContext.Name, packageReferenceContext.Version);
                    if (packageVersion != null)
                    {
                        var reference = new PackageReference
                        {
                            Package = packageVersion.Package,
                            Version = packageReferenceContext.Version,
                            ResolvedReference = packageVersion,
                            LineNumber = packageReferenceContext.LineNumber,
                            OriginalXml = packageReferenceContext.OriginalXml
                        };
                        project.PackageReferenceDictionary[reference.Package.Name] = reference;
                    }
                }));
            }

            if (context.HasPackagesConfig)
            {
                foreach (var packageContext in context.GetPackagesConfigContext().Packages)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        var packageVersion = await _packageVersionBuilder.GetOrCreatePackageVersion(packageContext.Id, packageContext.Version);
                        var reference = new PackageReference
                        {
                            Package = packageVersion.Package,
                            Version = packageContext.Version,
                            ResolvedReference = packageVersion,
                            LineNumber = packageContext.LineNumber,
                            OriginalXml = packageContext.OriginalXml
                        };

                        project.PackageReferenceDictionary[reference.Package.Name] = reference;
                    }));
                }
            }

            //foreach (var referenceContext in context.References.Where(c => c.IsPackage))
            //{
            //    if (_packageCache.PackagesDictionary.TryGetValue(referenceContext.Name, out var package))
            //    {
            //        if (!project.PackageReferenceDictionary.TryGetValue(package.Name, out var reference))
            //        {
            //            reference = new PackageReference
            //            {
            //                Package = package,
            //                Version = referenceContext.AssemblyVersion,
            //            };

            //            project.PackageReferenceDictionary[package.Name] = reference;
            //        }

            //        if (reference.AssemblyReferences == null)
            //        {
            //            reference.AssemblyReferences = new List<AssemblyReference>();
            //        }

            //        reference.AssemblyReferences.Add(new AssemblyReference
            //        {
            //            Package = package,
            //            HintPath = referenceContext.HintPath,
            //            Version = referenceContext.AssemblyVersion
            //        });
            //    }
            //    else
            //    {
            //        _logErrorAction(project, $"Package {referenceContext.Name} not found in cache");
            //    }
            //}

            //if (context.HasAppConfig)
            //{
            //    foreach (var assemblyRedirect in context.GetAppConfigContext().AssemblyRedirects)
            //    {
            //        project.BindingRedirections.Add(new BindingRedirection
            //        {
            //            AssemblyName = assemblyRedirect.AssemblyName,
            //            OldVersionFrom = assemblyRedirect.OldVersionFrom,
            //            OldVersionTo = assemblyRedirect.OldVersionTo,
            //            NewVersion = assemblyRedirect.NewVersion,
            //            LineNumber = assemblyRedirect.LineNumber
            //        });
            //    }
            //}



            await Task.WhenAll(tasks);

            return project;
        }
    }
}
