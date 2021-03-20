using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using GalaSoft.MvvmLight.Messaging;
using PackageDependencyAnalysis.Model;
using PackageDependencyAnalyzer.Messages;
using PackageDependencyAnalyzer.ViewModel;

namespace PackageDependencyAnalyzer.Controls
{
    /// <summary>
    /// Interaction logic for ProjectDetailControl.xaml
    /// </summary>
    public partial class ProjectDetailControl : UserControl
    {
        private ProjectViewModel ViewModel => (ProjectViewModel)DataContext;

        public ProjectDetailControl()
        {
            DataContextChanged += (sender, args) =>
            {
                PropertyGrid.SelectedObject = ViewModel;
            };

            Messenger.Default.Register<PackageReference>(this, reference =>
            {
                PackageReferences.SelectedItem = reference;
                PackageReferences.ScrollIntoView(reference);
            });

            InitializeComponent();

            Loaded += (sender, args) =>
            {
                Projects.RegisterSort(nameof(IProject.Name));
                PackageReferences.RegisterSort(nameof(PackageReference.Package), nameof(IPackage.Name));
                BindingRedirects.RegisterSort(nameof(BindingRedirection.AssemblyName));
                Dependencies.RegisterSort(nameof(IProject.Name));
            };
        }

        private void PackageReferenceDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement f && f.DataContext is PackageReference p)
            {
                if (p.ResolvedReference != null)
                {
                    Messenger.Default.Send(p.ResolvedReference);
                }
                else
                {
                    Messenger.Default.Send(p);
                }
            }
        }

        private void ProjectReferenceDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement f && f.DataContext is IProject p)
            {
                Messenger.Default.Send(p);
            }
        }

        private void PackageSearch_OnPopulating(object sender, PopulatingEventArgs e)
        {
            ((AutoCompleteBox)sender).ItemsSource = ViewModel.PackageReferences?.Select(s => s.Package.Name);
        }

        private void PackageSearch_OnDropDownClosed(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            var autoCompleteBox = ((AutoCompleteBox)sender);
            var key = autoCompleteBox.Text;
            if (ViewModel.PackageReferenceDictionary.TryGetValue(key, out var packageReference))
            {
                Dispatcher.Invoke(() =>
                {
                    ViewModel.SelectedPackageReference = packageReference;
                    autoCompleteBox.Text = string.Empty;
                    PackageReferences.ScrollIntoView(packageReference);
                });
            }
        }

        private void PackageSearchClear_Click(object sender, RoutedEventArgs e)
        {
            PackageSearch.Text = string.Empty;
        }

        private void AssemblySearch_OnPopulating(object sender, PopulatingEventArgs e)
        {
            ((AutoCompleteBox)sender).ItemsSource = ViewModel.BindingRedirections?.Select(s => s.AssemblyName);
        }

        private void AssemblySearch_OnDropDownClosed(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            var autoCompleteBox = ((AutoCompleteBox)sender);
            var key = autoCompleteBox.Text;
            var binding = ViewModel.BindingRedirections.SingleOrDefault(b => b.AssemblyName == key);
            if (binding != null)
            {
                Dispatcher.Invoke(() =>
                {
                    ViewModel.SelectedBindingRedirection = binding;
                    autoCompleteBox.Text = string.Empty;
                    PackageReferences.ScrollIntoView(binding);
                });
            }
        }

        private void AssemblySearchClear_Click(object sender, RoutedEventArgs e)
        {
            AssemblySearch.Text = string.Empty;
        }

        private void EditReferenceOnClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem m && m.DataContext is PackageReference p)
            {
                var dlg = new EditPackageReference { Project = ViewModel, PackageReference = p};
                if (dlg.ShowDialog() == true)
                {
                    Messenger.Default.Send(new ProjectModified{Project = ViewModel});
                }
            }
        }

        private void EditPackageReference_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem m && m.DataContext is PackageReference p)
            {
                var dlg = new XmlEditor { FileName = ViewModel.AbsolutePath, InitialLineNumber = p.LineNumber ?? 1, SelectMatchingText = p.Package.Name};
                if (dlg.ShowDialog() == true)
                {
                    //Messenger.Default.Send(new ProjectModified { Project = ViewModel });
                }
            }
        }

        private void EditPackagesConfigReference_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem m && m.DataContext is PackageReference p)
            {
                var dlg = new XmlEditor { FileName = ViewModel.PackagesConfigPath, InitialLineNumber = p.PackagesConfigLineNumber ?? 1, SelectMatchingText = p.Package.Name};
                if (dlg.ShowDialog() == true)
                {
                    //Messenger.Default.Send(new ProjectModified { Project = ViewModel });
                }
            }
        }

        private void EditAppConfigBinding_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem m && m.DataContext is BindingRedirection b)
            {
                var dlg = new XmlEditor { FileName = ViewModel.AppConfigPath, InitialLineNumber = b.LineNumber, SelectMatchingText = b.AssemblyName };
                if (dlg.ShowDialog() == true)
                {
                    //Messenger.Default.Send(new ProjectModified { Project = ViewModel });
                }
            }
        }
    }
}
