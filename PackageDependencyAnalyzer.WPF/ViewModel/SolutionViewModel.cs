using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
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
    public class SolutionViewModel : ViewModelBase, ISolution
    {
        private PackageCacheViewModel PackageCacheViewModel { get; } = ServiceLocator.Current.GetInstance<PackageCacheViewModel>();
        private LoggerViewModel LoggerViewModel => ServiceLocator.Current.GetInstance<LoggerViewModel>();
        private NamespaceViewModel _namespaceViewModel;
        private NamespaceViewModel _selectedNamespace;
        private ProjectViewModel _projectViewModel;
        private List<FileSystemWatcher> _watchers;
        private bool _isSolutionModified;

        public NamespaceViewModel NamespaceViewModel
        {
            get => _namespaceViewModel;
            set => Set(ref _namespaceViewModel, value);
        }

        public NamespaceViewModel SelectedNamespace
        {
            get => _selectedNamespace;
            set => Set(ref _selectedNamespace, value);
        }

        public ProjectViewModel ProjectViewModel
        {
            get => _projectViewModel;
            set
            {
                if (!(SelectedNamespace != null && SelectedNamespace.Projects.Contains(value)))
                {
                    var ns = NamespaceViewModel.Find(value, true);
                    if (ns != null)
                    {
                        ns.IsSelected = true;
                        Dispatcher.CurrentDispatcher.Invoke(() =>
                        {
                            SelectedNamespace = ns;
                            Set(ref _projectViewModel, value);
                        });
                        return;
                    }
                }
                Set(ref _projectViewModel, value);
            }
        }

        public string File { get; private set; }

        public IDictionary<string, IProject> ProjectCache { get; } = new ObservableDictionary<string, IProject>(){Dispatcher = Dispatcher.CurrentDispatcher};
        public ICollection<IProject> Projects => ProjectCache.Values;

        public bool IsSolutionModified
        {
            get => _isSolutionModified;
            set => Set(ref _isSolutionModified, value);
        }

        public SolutionViewModel()
        {
            if (IsInDesignMode)
            {
                NamespaceViewModel = new NamespaceViewModel
                {
                    Namespaces = new ObservableCollection<NamespaceViewModel>
                    {
                        new NamespaceViewModel
                        {
                            LocalName = "One"
                        }
                    }
                };

                SelectedNamespace = NamespaceViewModel.Namespaces.First();
            }
            else
            {
                NamespaceViewModel = new NamespaceViewModel
                {
                    Namespaces = new ObservableCollection<NamespaceViewModel>()
                };
            }

            ((INotifyCollectionChanged)Projects).CollectionChanged += (sender, args) =>
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        NamespaceViewModel.Insert(args.NewItems.OfType<IProject>());
                        break;
                    //case NotifyCollectionChangedAction.Remove:
                    //    Remove(args.OldItems);
                    //    break;
                    case NotifyCollectionChangedAction.Reset:
                        NamespaceViewModel.Namespaces.Clear();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };

            Messenger.Default.Register<IProject>(this, SelectProject);
        }

        public void Clear()
        {
            ProjectCache.Clear();
        }

        public void SetFile(string path)
        {
            File = path;

            if (_watchers != null)
            {
                foreach (var watcher in _watchers)
                {
                    watcher.Dispose();
                }
            }

            var folder = Path.GetDirectoryName(path);

            _watchers = new List<FileSystemWatcher>
            {
                CreateWatcher(folder, Path.GetFileName(path), OnFileChanged),
                CreateWatcher(folder, "*.csproj", OnFileChanged),
                CreateWatcher(folder, "app.config", OnFileChanged),
                CreateWatcher(folder, "packages.config", OnFileChanged)
            };

            Dispatcher.CurrentDispatcher.Invoke(() => IsSolutionModified = false);
        }

        public void LoadFromFile(string path)
        {
            var solution = SolutionContext.Create(path);

            ProjectCache.Clear();

            var builder = new ProjectBuilder(this, PackageCacheViewModel, LoggerViewModel.Error);

            foreach (var projectContext in solution.Projects)
            {
                //GetProject(solution, solutionProject.FileName);
                ProjectCache[projectContext.FileName] = builder.Build(new ProjectViewModel(), projectContext);
            }

            builder.AddProjectReferences(solution);

            SetFile(path);
        }

        private FileSystemWatcher CreateWatcher(string path, string filter, Action<string> onChange)
        {
            var watcher = new FileSystemWatcher
            {
                Path = path,
                Filter = filter,
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
            };

            watcher.Changed += (sender, args) => onChange(args.Name);
            watcher.Created += (sender, args) => onChange(args.Name);
            watcher.Deleted += (sender, args) => onChange(args.Name);
            watcher.Renamed += (sender, args) => onChange(args.Name);

            watcher.EnableRaisingEvents = true;

            return watcher;
        }

        private void OnFileChanged(string fileName)
        {
            Dispatcher.CurrentDispatcher.Invoke(() => IsSolutionModified = true);
        }

        private IProject GetProject(SolutionContext context, string name)
        {
            if (!ProjectCache.TryGetValue(name, out var project))
            {
                var projectContext = context.GetProject(name);

                project = new ProjectViewModel
                {
                    AbsolutePath = projectContext.FileName,
                    Name = projectContext.Name,
                    Version = projectContext.Version
                };
                ProjectCache[projectContext.FileName] = project;

                foreach (var packageReferenceContext in projectContext.PackageReferences)
                {
                    if (!PackageCacheViewModel.PackagesDictionary.TryGetValue(packageReferenceContext.Name, out var package))
                    {
                        package = new PackageViewModel(packageReferenceContext.Name);
                        PackageCacheViewModel.PackagesDictionary[packageReferenceContext.Name] = package;
                    }

                    var reference = new PackageReference
                    {
                        Package = package,
                        Version = packageReferenceContext.Version,
                        AssemblyReferences = packageReferenceContext.AssemblyReferences.Select(r => new AssemblyReference
                        {
                            Version = r.AssemblyVersion,
                            Package = package
                        }).ToList()
                    };

                    if (project.PackageReferenceDictionary.TryGetValue(reference.Package.Name, out var existingReference))
                    {
                        LoggerViewModel.Error(project,
                            $"There is already a reference to package {existingReference.Package.Name} at version {existingReference.Version}");
                    }

                    project.PackageReferenceDictionary[reference.Package.Name] = reference;
                }

                var packagesConfigPackageContexts = projectContext.GetPackagesConfigContext()?.Packages;
                if (packagesConfigPackageContexts != null)
                {
                    foreach (var package in packagesConfigPackageContexts)
                    {
                        if (!project.PackageReferenceDictionary.ContainsKey(package.Id))
                        {
                            project.PackageReferenceDictionary[package.Id] = new PackageReference
                            {

                            };
                        }
                    }
                }

                foreach (var projectReferenceContext in projectContext.ProjectReferences)
                {
                    if (!ProjectCache.TryGetValue(projectReferenceContext.FileName, out var subProject))
                    {
                        subProject = GetProject(context, projectReferenceContext.FileName);
                    }

                    project.Projects.Add(subProject);
                    subProject.Dependencies.Add(project);
                }
            }

            return project;
        }

        private void SelectProject(IProject obj)
        {
            ProjectViewModel = (ProjectViewModel) obj;
        }
    }
}
