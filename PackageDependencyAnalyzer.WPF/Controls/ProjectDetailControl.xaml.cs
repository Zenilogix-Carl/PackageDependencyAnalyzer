using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

        private void Search_OnPopulating(object sender, PopulatingEventArgs e)
        {
            ((AutoCompleteBox)sender).ItemsSource = ViewModel.PackageReferences?.Select(s => s.Package.Name);
        }

        private void Search_OnDropDownClosed(object sender, RoutedPropertyChangedEventArgs<bool> e)
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

        private void SearchClear_Click(object sender, RoutedEventArgs e)
        {
            Search.Text = string.Empty;
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
    }
}
