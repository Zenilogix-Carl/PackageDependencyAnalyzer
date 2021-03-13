using System.Collections;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GalaSoft.MvvmLight.Messaging;
using PackageDependencyAnalysis.Model;
using PackageDependencyAnalysis.Processors;

namespace PackageDependencyAnalyzer.Controls
{
    /// <summary>
    /// Interaction logic for PackageReferences.xaml
    /// </summary>
    public partial class PackageReferences : Window
    {
        public IPackage Package { get; set; }
        public IPackageVersion PackageVersion { get; set; }

        public PackageReferences()
        {
            InitializeComponent();

            Loaded += OnLoaded;

        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            Title =
                $"{Package.Name} {(PackageVersion == null ? "" : PackageVersion.Version+" ")}- All Package References";

            if (PackageVersion != null)
            {
                ListView.ItemsSource = PackageCacheProcessor.GetDependentPackages(PackageVersion);
            }
            else
            {
                ListView.ItemsSource = PackageCacheProcessor.GetDependentPackages(Package);
            }
        }

        private void PackageReferenceDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is ListViewItem lvi)
            {
                if (lvi.DataContext is IPackageVersion v)
                {
                    Messenger.Default.Send(v);
                }
                else if (lvi.DataContext is IPackage p)
                {
                    Messenger.Default.Send(p);
                }
            }
        }

        private void CopyToClipboard_OnClick(object sender, RoutedEventArgs e)
        {
            var sb = new StringBuilder();
            if (PackageVersion != null)
            {
                sb.AppendLine("Package\tVersion\tPre-Release");
            }

            foreach (var item in ListView.ItemsSource)
            {
                if (item is IPackage p)
                {
                    sb.AppendLine(p.Name);
                }
                else if (item is IPackageVersion v)
                {
                    sb.AppendLine($"{v.Package.Name}\t{v.Version}\t{v.PreReleaseSuffix}");
                }
            }

            Clipboard.SetText(sb.ToString());
            DialogResult = false;
        }
    }
}
