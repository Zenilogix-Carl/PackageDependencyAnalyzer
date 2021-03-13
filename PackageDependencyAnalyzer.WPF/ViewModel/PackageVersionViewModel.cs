using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using GalaSoft.MvvmLight;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalyzer.ViewModel
{
    public class PackageVersionViewModel : ViewModelBase, IPackageVersion
    {
        [ReadOnly(true)]
        public string File { get; }
        [Browsable(false)]
        public IPackage Package { get; set; }

        public string Name => Package.Name;
        public string Folder => Path.GetDirectoryName(File);

        [ReadOnly(true)]
        public DateTime DateTime { get; set; }

        [ReadOnly(true)]
        public string Description { get; set; }

        [ReadOnly(true)]
        public Version Version { get; set; }

        [Browsable(false)]
        public bool IsPrerelease { get; set; }

        [ReadOnly(true)]
        public string PreReleaseSuffix { get; set; }

        [Browsable(false)]
        public PlatformDependencies Dependencies { get; } = new PlatformDependencies();
        [Browsable(false)]
        public ICollection<IProject> ReferencingProjects { get; } = new ObservableCollection<IProject>();

        [Browsable(false)]
        public ICollection<IProject> ConfigReferences { get; } = new ObservableCollection<IProject>();
        [Browsable(false)]
        public ICollection<IProject> BindingRedirectReferences { get; } = new ObservableCollection<IProject>();

        [Browsable(false)]
        public ICollection<IPackageVersion> ReferencingPackages { get; } = new ObservableCollection<IPackageVersion>();

        [Browsable(false)]
        public string NuSpec { get; set; }

        [ReadOnly(true)]
        public string RepositoryUrl { get; set; }

        [Browsable(false)]
        public bool IsValidRepositoryUrl =>
            (Uri.TryCreate(RepositoryUrl, UriKind.Absolute, out var uri) &&
             uri.Scheme.StartsWith("http", StringComparison.InvariantCultureIgnoreCase));

        [Browsable(false)]
        public bool IsReferenced { get; set; }

        [Browsable(false)]
        public new bool IsInDesignMode { get; set; }

        public PackageVersionViewModel(string file)
        {
            File = file;
        }
        public override string ToString() => $"{Package.Name} {Version} ({Dependencies.SelectMany(s => s.Value).Count()} dependencies)";
    }
}
