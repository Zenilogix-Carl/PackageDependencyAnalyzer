using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using GalaSoft.MvvmLight;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalyzer.ViewModel
{
    public class NamespaceViewModel : ViewModelBase
    {
        private ObservableCollection<NamespaceViewModel> _namespaces;
        private ObservableCollection<IProject> _projects;
        private string _localName;
        private bool _isSelected;
        private bool _isExpanded;

        public string LocalName
        {
            get => _localName;
            set
            {
                Set(ref _localName, value);
                RaisePropertyChanged(nameof(DisplayName));
            }
        }

        public string DisplayName => LocalName ?? "(none)";

        public bool IsSelected
        {
            get => _isSelected;
            set => Set(ref _isSelected, value);
        }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => Set(ref _isExpanded, value);
        }

        public ObservableCollection<NamespaceViewModel> Namespaces
        {
            get => _namespaces;
            set => Set(ref _namespaces, value);
        }

        public ObservableCollection<IProject> Projects
        {
            get => _projects;
            set => Set(ref _projects, value);
        }

        public NamespaceViewModel()
        {
            Namespaces = new ObservableCollection<NamespaceViewModel>();
            Projects = new ObservableCollection<IProject>();
            BindingOperations.EnableCollectionSynchronization(Namespaces, this);
            BindingOperations.EnableCollectionSynchronization(Projects, this);
        }

        public NamespaceViewModel Find(IProject project, bool expandParent=false)
        {
            if (Projects.Contains(project))
            {
                return this;
            }

            foreach (var namespaceViewModel in Namespaces)
            {
                var vm = namespaceViewModel.Find(project, expandParent);
                if (vm != null)
                {
                    if (expandParent)
                    {
                        namespaceViewModel.IsExpanded = true;
                    }
                    return vm;
                }
            }

            return null;
        }

        public void Merge()
        {
            foreach (var namespaceViewModel in Namespaces)
            {
                namespaceViewModel.Merge();
            }

            if (Projects.Count == 0 && Namespaces.Count == 1 && !string.IsNullOrEmpty(LocalName))
            {
                var child = Namespaces.Single();
                Projects = child.Projects;
                LocalName = $"{LocalName}.{child.LocalName}";
                Namespaces = child.Namespaces;
            }
        }

        public void Insert(IEnumerable<IProject> projects)
        {
            foreach (var project in projects)
            {
                Insert(project);
            }
        }

        public void Insert(IProject project)
        {
            var namespaceParts = project.Name.Split('.');
            if (namespaceParts.Length == 1)
            {
                // No namespace
                var nullNamespace = Namespaces.SingleOrDefault(s => string.IsNullOrEmpty(s.LocalName));
                if (nullNamespace == null)
                {
                    nullNamespace = new NamespaceViewModel();
                    Namespaces.Insert(0, nullNamespace);
                }
                nullNamespace.Insert(project, new string[]{}, 0);
            }
            Insert(project, namespaceParts.Take(namespaceParts.Length-1).ToArray(), 0);
        }

        private void Insert(IProject project, string[] namespaceParts, int baseIndex)
        {
            if (baseIndex >= namespaceParts.Length)
            {
                Projects.Add(project);
            }
            else
            {
                var localName = namespaceParts[baseIndex];
                var fullName = string.Join(".", namespaceParts.Take(baseIndex + 1));
                var subItem = Namespaces.SingleOrDefault(s => s.LocalName == localName);
                if (subItem == null)
                {
                    subItem = new NamespaceViewModel{LocalName = localName};
                    Namespaces.SortedInsert(subItem, a => a.DisplayName);
                }
                subItem.Insert(project, namespaceParts, baseIndex+1);
            }
        }
    }
}
