using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalyzer.ViewModel
{
    public class PackageCacheViewModel : ViewModelBase, IPackageCache
    {
        private IPackage _selectedPackage;
        private IPackageVersion _selectedPackageVersion;
        private string _issues;
        private ICollection<IPackage> _referencedPackages;

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
