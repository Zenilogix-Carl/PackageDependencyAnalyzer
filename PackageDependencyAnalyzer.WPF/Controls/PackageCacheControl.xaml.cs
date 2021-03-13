using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight.Messaging;
using PackageDependencyAnalysis.Model;
using PackageDependencyAnalyzer.ViewModel;

namespace PackageDependencyAnalyzer.Controls
{
    /// <summary>
    /// Interaction logic for PackageCacheControl.xaml
    /// </summary>
    public partial class PackageCacheControl : UserControl
    {
        private PackageCacheViewModel ViewModel => (PackageCacheViewModel) DataContext;

        public PackageCacheControl()
        {
            InitializeComponent();

            Messenger.Default.Register<IPackage>(this, SelectPackage);
        }

        private void SelectPackage(IPackage package)
        {
            ViewModel.SelectedPackage = package;
        }

        private void Packages_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView lv && e.AddedItems.Count > 0)
            {
                lv.ScrollIntoView(e.AddedItems[0]);
            }
        }

        //private void AllCache_OnPopulating(object sender, PopulatingEventArgs e)
        //{
        //    ((AutoCompleteBox)sender).ItemsSource = ViewModel.PackagesCache.Select(s => s.Value.Name);
        //}

        //private void AllCacheClear_Click(object sender, RoutedEventArgs e)
        //{
        //    AllCache.Text = string.Empty;
        //}

        private void Active_OnPopulating(object sender, PopulatingEventArgs e)
        {
            ((AutoCompleteBox)sender).ItemsSource = ViewModel.ReferencedPackages?.Select(s => s.Name);
        }

        private void ActiveClear_Click(object sender, RoutedEventArgs e)
        {
            Active.Text = string.Empty;
        }

        private void All_OnPopulating(object sender, PopulatingEventArgs e)
        {
            ((AutoCompleteBox)sender).ItemsSource = ViewModel.Packages?.Select(s => s.Name);
        }

        private void AllClear_Click(object sender, RoutedEventArgs e)
        {
            All.Text = string.Empty;
        }

        private void Search_OnDropDownClosed(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            var autoCompleteBox = ((AutoCompleteBox) sender);
            var key = autoCompleteBox.Text;
            if (ViewModel.PackagesDictionary.TryGetValue(key, out var package))
            {
                Dispatcher.Invoke(() =>
                {
                    ViewModel.SelectedPackage = package;
                    if (package.PackageVersions.Count == 1)
                    {
                        ViewModel.SelectedPackageVersion = package.PackageVersions.Single();
                    }

                    autoCompleteBox.Text = string.Empty;
                });

                if (sender == Active)
                {
                    ActiveList.ScrollIntoView(package);
                }
                else if (sender == All)
                {
                    AllList.ScrollIntoView(package);
                }
                //else if (sender == AllCache)
                //{
                //    CacheList.ScrollIntoView(package);
                //}
            }
        }

        private void FindAllReferencesOnClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem m && m.DataContext is PackageViewModel p)
            {
                var dlg = new PackageReferences {Package = p};
                dlg.ShowDialog();
            }
        }

        private void CopyNameToClipboardClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem m && m.DataContext is PackageViewModel p)
            {
                Clipboard.SetText(p.Name);
            }
        }
    }
}
