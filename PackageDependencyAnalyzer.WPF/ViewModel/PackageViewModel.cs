using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalyzer.ViewModel
{
    public class PackageViewModel : ViewModelBase, IPackage
    {
        public string Name { get; }
        public ICollection<ReleaseVersion> Versions => VersionDictionary.Keys;
        public ICollection<IPackageVersion> PackageVersions => VersionDictionary.Values;

        public IDictionary<ReleaseVersion, IPackageVersion> VersionDictionary { get; } =
            new ObservableDictionary<ReleaseVersion, IPackageVersion>{Dispatcher = Dispatcher.CurrentDispatcher};

        public IDictionary<string, IProject> ReferencingProjects { get; } = new ObservableDictionary<string, IProject>{Dispatcher = Dispatcher.CurrentDispatcher};
        public IDictionary<string, IPackageVersion> ReferencingPackages { get; } = new ObservableDictionary<string, IPackageVersion>{Dispatcher = Dispatcher.CurrentDispatcher};
        public bool IsReferenced { get; set; }
        public bool HasVersions { get; private set; }

        public ICollection<IPackageVersion> ReferencedVersions =>
            VersionDictionary.Values.Where(v => v.IsReferenced).ToList();

        public PackageViewModel(string name)
        {
            Name = name;
            if (VersionDictionary is INotifyCollectionChanged c)
            {
                c.CollectionChanged += (sender, args) =>
                {
                    HasVersions = VersionDictionary.Any();
                };
            }
        }

        public override string ToString() => $"{Name} ({VersionDictionary.Count} version(s)) {string.Join(", ", Versions)}";
    }
}
