using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GalaSoft.MvvmLight.Messaging;
using PackageDependencyAnalysis.Model;
using PackageDependencyAnalysis.Processors;
using PackageDependencyAnalyzer.ViewModel;

namespace PackageDependencyAnalyzer.Controls
{
    /// <summary>
    /// Interaction logic for PackageVersionControl.xaml
    /// </summary>
    public partial class PackageVersionControl : UserControl
    {
        private PackageCacheViewModel ViewModel => (PackageCacheViewModel) DataContext;

        public PackageVersionControl()
        {
            DataContextChanged += (sender, args) =>
            {
                if (args.OldValue != null)
                {
                    ViewModel.PropertyChanged -= ViewModelOnPropertyChanged;
                }
                if (args.NewValue != null)
                {
                    ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
                }
            };

            InitializeComponent();
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PackageCacheViewModel.SelectedPackageVersion))
            {
                PropertyGrid.SelectedObject = ViewModel.SelectedPackageVersion;
            }
        }

        private void PackageReferenceDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement f && f.DataContext is IPackageVersion v)
            {
                Dispatcher.Invoke(() =>
                {
                    ViewModel.SelectedPackage = v.Package;
                    ViewModel.SelectedPackageVersion = v;
                    TabControl.SelectedItem = Details;
                });
            }
        }

        private void ProjectReferenceDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement f && f.DataContext is IProject p)
            {
                Dispatcher.Invoke(() =>
                {
                    Messenger.Default.Send(p);
                    var pv = ViewModel.SelectedPackageVersion;
                    var reference = p.PackageReferences.FirstOrDefault(r =>
                        r.Package == pv.Package);
                    if (reference != null)
                    {
                        Messenger.Default.Send(reference);
                    }
                });
            }
        }

        private void OnTreeViewDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TreeViewItem t && t.DataContext is PackageReference p)
            {
                Dispatcher.Invoke(() =>
                {
                    ViewModel.SelectedPackage = p.Package;
                    ViewModel.SelectedPackageVersion = p.ResolvedReference;
                    TabControl.SelectedItem = Details;
                });
            }
        }
    }
}
