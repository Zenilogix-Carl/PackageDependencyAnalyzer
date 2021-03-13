using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Threading;
using GalaSoft.MvvmLight;
using PackageDependencyAnalysis.Model;

namespace PackageDependencyAnalyzer.ViewModel
{
    public class ProjectViewModel : ViewModelBase, IProject
    {
        private PackageReference _selectedPackageReference;
        private BindingRedirection _selectedBindingRedirection;

        [ReadOnly(true),
         DisplayName("Full Path Name")]
        public string File => AbsolutePath;

        [ReadOnly(true),
        DisplayName("Project Name")]
        public string Name { get; set; }

        [Browsable(false)]
        public string AbsolutePath { get; set; }

        [Browsable(false)]
        public string PackagesConfigPath { get; set; }

        [Browsable(false)]
        public string AppConfigPath { get; set; }

        [ReadOnly(true)]
        public Version Version { get; set; }

        [ReadOnly(true),
        DisplayName("Containing Folder")]
        public string Folder => Path.GetDirectoryName(File);

        [ReadOnly(true),
        DisplayName("File Name")]
        public string FileName => Path.GetFileName(File);

        [Browsable(false)]
        public ICollection<IProject> Projects { get; } = new ObservableCollection<IProject>();
        [Browsable(false)]
        public ICollection<IProject> Dependencies { get; } = new ObservableCollection<IProject>();
        [Browsable(false)]
        public IDictionary<string,PackageReference> PackageReferenceDictionary { get; } = new ObservableDictionary<string, PackageReference>{Dispatcher = Dispatcher.CurrentDispatcher};
        [Browsable(false)]
        public ICollection<PackageReference> PackageReferences => PackageReferenceDictionary.Values;
        [Browsable(false)]
        public ICollection<BindingRedirection> BindingRedirections { get; } = new ObservableCollection<BindingRedirection>();

        [Browsable(false)]
        public string EditMenuString => $"Edit Project {FileName}";

        [Browsable(false)]
        public PackageReference SelectedPackageReference
        {
            get => _selectedPackageReference;
            set => Set(ref _selectedPackageReference, value);
        }

        [Browsable(false)]
        public BindingRedirection SelectedBindingRedirection
        {
            get => _selectedBindingRedirection;
            set => Set(ref _selectedBindingRedirection, value);
        }

        [Browsable(false)]
        public new bool IsInDesignMode { get; set; }
        public override string ToString() => $"{Name} ({Projects.Count} project references, {PackageReferences.Count} package references)";
    }
}
