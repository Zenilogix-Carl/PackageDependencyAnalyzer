using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommonServiceLocator;
using GalaSoft.MvvmLight;
using PackageDependencyAnalysis.Analyzers;
using PackageDependencyAnalysis.Model;
using PackageDependencyAnalyzer.Properties;

namespace PackageDependencyAnalyzer.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private string _solutionFile;
        private bool _isBusy;

        public string SolutionFile
        {
            get => _solutionFile;
            set
            {
                Set(ref _solutionFile, value);
                RaisePropertyChanged(nameof(Title));
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => Set(ref _isBusy, value);
        }

        public ObservableCollection<string> RecentFiles { get; }
        public bool HasRecentFiles => RecentFiles.Any();

        private PackageCacheViewModel PackageCacheViewModel => ServiceLocator.Current.GetInstance<PackageCacheViewModel>();
        private SolutionViewModel SolutionViewModel => ServiceLocator.Current.GetInstance<SolutionViewModel>();
        private LoggerViewModel LoggerViewModel => ServiceLocator.Current.GetInstance<LoggerViewModel>();

        public string Title => $"{(string.IsNullOrEmpty(_solutionFile)?"":$"{Path.GetFileNameWithoutExtension(_solutionFile)} - ")}Package Dependency Analyzer";

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            if (IsInDesignMode)
            {
                RecentFiles = new ObservableCollection<string>(new []{"One","Two","Three"});
                SolutionFile = "Sample.sln";
            }
            else
            {
                RecentFiles = new ObservableCollection<string>((Settings.Default.RecentFiles ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            }

            // Messenger.Default.Register<ProjectModified>(this, message => HandleProjectFileChange(message.Project));
        }

        private async void HandleProjectFileChange(IProject project)
        {
            await LoadAsync();
        }

        public async Task OpenSolution(string path)
        {
            SolutionFile = path;
            await LoadAsync();
            if (RecentFiles.Contains(path))
            {
                RecentFiles.Remove(path);
            }
            RecentFiles.Insert(0, path);
            RaisePropertyChanged(nameof(RecentFiles));
            RaisePropertyChanged(nameof(HasRecentFiles));
            Settings.Default.RecentFiles = string.Join(";", RecentFiles.Take(10));
        }

        public async Task LoadAsync()
        {
            await Task.Run(() =>
            {
                IsBusy = true;
                SolutionViewModel.Clear();
                PackageCacheViewModel.Clear();
                LoggerViewModel.Clear();
                PackageCacheViewModel.Issues = string.Empty;

                PackageCacheViewModel.LoadForSolution(SolutionFile, s => s.StartsWith("net") || s.Contains("portable"));

                SolutionViewModel.LoadFromFile(SolutionFile);

                SolutionViewModel.NamespaceViewModel.Merge();

                var resolver = new ReferenceResolver(SolutionViewModel, PackageCacheViewModel, LoggerViewModel.Warning);
                resolver.ResolveAllReferences();
                PackageCacheViewModel.ReferencedPackages = DependencyAnalyzer.GetReferencedPackages(PackageCacheViewModel);

                PackageCacheViewModel.Issues = string.Join("\r\n", IssueScanner.Scan(PackageCacheViewModel));

                IsBusy = false;
            });
        }
    }
}