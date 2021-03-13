using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Threading;
using CommonServiceLocator;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using PackageDependencyAnalysis.Analyzers;
using PackageDependencyAnalysis.ContextObjects;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalyzer.ViewModel
{
    public class PackageCacheViewModel : ViewModelBase, IPackageCache
    {
        private LoggerViewModel LoggerViewModel => ServiceLocator.Current.GetInstance<LoggerViewModel>();

        private IPackage _selectedPackage;
        private IPackageVersion _selectedPackageVersion;
        private string _issues;
        private ICollection<IPackage> _referencedPackages;
        private AssemblyInfo _selectedAssemblyInfo;

        public IDictionary<string, IPackage> PackagesDictionary { get; } = new ObservableDictionary<string, IPackage>(){Dispatcher = Dispatcher.CurrentDispatcher};
        public ICollection<IPackage> Packages => PackagesDictionary.Values;

        public ICollection<IPackage> ReferencedPackages
        {
            get => _referencedPackages;
            set => Set(ref _referencedPackages, value);
        }

        public ICollection<IPackage> ExistingPackages => Packages.Where(c => c.PackageVersions.Count > 0).ToList();

        public IPackage SelectedPackage
        {
            get => _selectedPackage;
            set => Set(ref _selectedPackage, value);
        }

        public IPackageVersion SelectedPackageVersion
        {
            get => _selectedPackageVersion;
            set => Set(ref _selectedPackageVersion, value);
        }

        public AssemblyInfo SelectedAssemblyInfo
        {
            get => _selectedAssemblyInfo;
            set => Set(ref _selectedAssemblyInfo, value);
        }

        public string Issues
        {
            get => _issues;
            set
            {
                Set(ref _issues, value);
                RaisePropertyChanged(nameof(HasIssues));
            }
        }

        public bool HasIssues => !string.IsNullOrEmpty(Issues);

        public PackageCacheViewModel()
        {
            Messenger.Default.Register<IPackageVersion>(this, SelectPackageVersion);
            Messenger.Default.Register<PackageReference>(this, SelectPackage);

            ((INotifyCollectionChanged)Packages).CollectionChanged += (sender, args) =>
            {
                RaisePropertyChanged(nameof(ExistingPackages));
            };
        }

        public void Clear()
        {
            PackagesDictionary.Clear();
        }

        public void LoadForSolution(string solutionFileName, Func<string,bool> frameworkFilter)
        {
            var context = new PackageCacheContext(solutionFileName, frameworkFilter);
            foreach (var packageContext in context.Packages)
            {
                if (!PackagesDictionary.TryGetValue(packageContext.Name, out var package))
                {
                    package = new PackageViewModel(packageContext.Name);
                    PackagesDictionary[packageContext.Name] = package;
                }

                var packageVersion = new PackageVersionViewModel(packageContext.FileName)
                {
                    Version = packageContext.Version,
                    PreReleaseSuffix = packageContext.PreReleaseSuffix,
                    DateTime = packageContext.DateTime,
                    Description = packageContext.Description,
                    NuSpec = packageContext.RawNuSpec,
                    RepositoryUrl = packageContext.RepositoryUrl,
                    Package = package,
                };

                foreach (var packageAssembly in packageContext.Assemblies)
                {
                    packageVersion.Assemblies.Add(new AssemblyInfo
                    {
                        Name = packageAssembly.FileName,
                        Frameworks = packageAssembly.Frameworks.ToList()
                    });
                }

                foreach (var dependency in packageContext.Dependencies)
                {
                    if (!packageVersion.Dependencies.TryGetValue(string.Empty, out var frameworkDependencies))
                    {
                        frameworkDependencies = new Dependencies();
                        packageVersion.Dependencies[string.Empty] = frameworkDependencies;
                    }

                    frameworkDependencies[dependency.Name] = CreatePackageReference(dependency);
                }

                foreach (var dependencyGroup in packageContext.Groups)
                {
                    if (!packageVersion.Dependencies.TryGetValue(dependencyGroup.TargetFramework, out var frameworkDependencies))
                    {
                        frameworkDependencies = new Dependencies();
                        packageVersion.Dependencies[dependencyGroup.TargetFramework] = frameworkDependencies;
                    }

                    foreach (var reference in dependencyGroup.References)
                    {
                        frameworkDependencies[reference.Name] = CreatePackageReference(reference);
                    }
                }

                package.VersionDictionary[packageVersion.Version] = packageVersion;
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

            LoggerViewModel.Warning(context,
                $"Could not resolve package reference {packageReference.Package.Name} {packageReference.Version}; available versions: {string.Join(",", package.Versions)}");
            return false;
        }

        public void ResolveBindingRedirection(IProject project, BindingRedirection bindingRedirection)
        {
            if (PackagesDictionary.TryGetValue(bindingRedirection.AssemblyName, out var package))
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

        private PackageReference CreatePackageReference(NuSpecReference reference)
        {
            if (!PackagesDictionary.TryGetValue(reference.Name, out var package))
            {
                package = new PackageViewModel(reference.Name);

                PackagesDictionary[reference.Name] = package;
            }

            return new PackageReference
            {
                Package = package,
                Version = reference.Version,
                IsPrerelease = reference.IsPreRelease,
                PreReleaseSuffix = reference.PreRelease
            };
        }

        private void SelectPackage(PackageReference obj)
        {
            SelectedPackage = obj.Package;
        }

        private void SelectPackageVersion(IPackageVersion obj)
        {
            SelectedPackage = obj.Package;
            SelectedPackageVersion = obj;
        }
    }
}
