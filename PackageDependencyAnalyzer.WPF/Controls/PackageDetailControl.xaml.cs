using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GalaSoft.MvvmLight.Messaging;
using PackageDependencyAnalysis.Model;
using PackageDependencyAnalyzer.ViewModel;

namespace PackageDependencyAnalyzer.Controls
{
    /// <summary>
    /// Interaction logic for PackageDetailControl.xaml
    /// </summary>
    public partial class PackageDetailControl : UserControl
    {
        private PackageCacheViewModel ViewModel => (PackageCacheViewModel) DataContext;

        public PackageDetailControl()
        {
            InitializeComponent();
        }

        private void PackageReferenceDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement f && f.DataContext is KeyValuePair <string,IPackageVersion> kvp)
            {
                Dispatcher.Invoke(() =>
                {
                    ViewModel.SelectedPackage = kvp.Value.Package;
                    ViewModel.SelectedPackageVersion = kvp.Value;
                    TabControl.SelectedItem = Versions;
                });
            }
        }

        private void ProjectReferenceDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem i && i.DataContext is KeyValuePair<string,IProject> kvp)
            {
                Dispatcher.Invoke(() =>
                {
                    Messenger.Default.Send(kvp.Value);
                    var pv = ViewModel.SelectedPackageVersion;
                    var reference = kvp.Value.PackageReferences.FirstOrDefault(r =>
                        r.Package == pv?.Package) ?? kvp.Value.PackageReferences.FirstOrDefault(r =>
                        r.Package == ViewModel.SelectedPackage);
                    if (reference != null)
                    {
                        Messenger.Default.Send(reference);
                    }
                });
            }
        }

        private void OpenRepository_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem m && m.DataContext is IPackageVersion p)
            {
                Process.Start(p.RepositoryUrl);
            }
        }

        private void FindAllReferencesOnClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem m && m.DataContext is PackageVersionViewModel v)
            {
                var dlg = new PackageReferences {Package = v.Package, PackageVersion = v};
                dlg.ShowDialog();
            }
        }
    }
}
