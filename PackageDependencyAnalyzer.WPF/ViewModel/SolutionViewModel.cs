using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalyzer.ViewModel
{
    public class SolutionViewModel : ViewModelBase, ISolution
    {
        private NamespaceViewModel _namespaceViewModel;
        private NamespaceViewModel _selectedNamespace;
        private ProjectViewModel _projectViewModel;

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

        public void SetFile(string path)
        {
            File = path;
        }

        public IDictionary<string, IProject> ProjectCache { get; } = new ObservableDictionary<string, IProject>(){Dispatcher = Dispatcher.CurrentDispatcher};
        public ICollection<IProject> Projects => ProjectCache.Values;

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
                        Add(args.NewItems);
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
            //Projects.Clear();
        }

        private void SelectProject(IProject obj)
        {
            ProjectViewModel = (ProjectViewModel) obj;
        }

        private void Remove(IList argsOldItems)
        {
            throw new NotImplementedException();
        }

        private void Add(IList argsNewItems)
        {
            NamespaceViewModel.Insert(argsNewItems.OfType<IProject>());
        }
    }
}
