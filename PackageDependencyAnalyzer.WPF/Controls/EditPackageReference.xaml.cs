using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using PackageDependencyAnalysis.Model;
using PackageDependencyAnalysis.Processors;
using PackageDependencyAnalyzer.Messages;
using PackageDependencyAnalyzer.ViewModel;

namespace PackageDependencyAnalyzer.Controls
{
    /// <summary>
    /// Interaction logic for EditProjectReference.xaml
    /// </summary>
    public partial class EditPackageReference : Window
    {
        private object _appConfigDoc;
        private object _appConfigReference;
        private object _projectDoc;
        private object _projectReference;
        private object _packagesDoc;
        private object _packageReference;
        private PackageReference _packageReferenceOriginal;
        private PackageReference _packageReferenceEdit;
        public IProject Project { get; set; }

        public PackageReference PackageReference
        {
            get => _packageReferenceOriginal;
            set
            {
                _packageReferenceOriginal = value;
                _packageReferenceEdit = value.Clone();
            }
        }

        private EditPackageReferenceViewModel ViewModel { get; set; }

        public EditPackageReference()
        {
            InitializeComponent();

            Loaded += OnLoaded;

            Messenger.Default.Register<ProjectPackageReferenceModified>(this, OnProjectPackageReferenceModified);
            Messenger.Default.Register<BindingRedirectModified>(this, OnBindingRedirectionModified);
        }

        private void OnBindingRedirectionModified(BindingRedirectModified obj)
        {
            if (!string.IsNullOrEmpty(AppConfigXML.Text))
            {
                AppConfigXML.Text = AppConfigFileProcessor.ReplacePackageReference(_appConfigDoc, _appConfigReference, _packageReferenceEdit);
                AppConfigCheck.IsChecked = true;
            }
        }

        private void OnProjectPackageReferenceModified(ProjectPackageReferenceModified obj)
        {
            ProjectXML.Text = ProjectFileProcessor.ReplacePackageReference( _projectDoc, _projectReference, _packageReferenceEdit);
            ProjectCheck.IsChecked = true;
            if (!string.IsNullOrEmpty(PackagesConfigXML.Text))
            {
                PackagesConfigXML.Text = PackagesConfigFileProcessor.ReplacePackageReference(_packagesDoc, _packageReference, _packageReferenceEdit);
                PackagesConfigCheck.IsChecked = true;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            OriginalProjectXML.Text = ProjectFileProcessor.GetPackageReference(Project, PackageReference, out _projectDoc, out _projectReference);
            OriginalPackagesConfigXML.Text = PackagesConfigFileProcessor.GetPackageReference(Project, PackageReference, out _packagesDoc, out _packageReference);
            OriginalAppConfigXML.Text = AppConfigFileProcessor.GetPackageReference(Project, PackageReference, out _appConfigDoc, out _appConfigReference);
            ProjectXML.Text = OriginalProjectXML.Text;
            PackagesConfigXML.Text = OriginalPackagesConfigXML.Text;
            AppConfigXML.Text = OriginalAppConfigXML.Text;
            ViewModel = new EditPackageReferenceViewModel(Project, _packageReferenceEdit);
            PropertyGrid.SelectedObject = ViewModel;
            AppConfigCheck.Visibility = string.IsNullOrWhiteSpace(OriginalAppConfigXML.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
            PackagesConfigCheck.Visibility = string.IsNullOrWhiteSpace(PackagesConfigXML.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
            ProjectCheck.Visibility =
                string.IsNullOrWhiteSpace(OriginalAppConfigXML.Text) &&
                string.IsNullOrWhiteSpace(PackagesConfigXML.Text)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
            AppConfigTab.Visibility = string.IsNullOrWhiteSpace(OriginalAppConfigXML.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
            PackagesConfigTab.Visibility = string.IsNullOrWhiteSpace(PackagesConfigXML.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
            Apply.Visibility =
                string.IsNullOrWhiteSpace(OriginalAppConfigXML.Text) &&
                string.IsNullOrWhiteSpace(PackagesConfigXML.Text)
                    ? Visibility.Collapsed
                    : Visibility.Visible;
        }

        private void Save_OnClick(object sender, RoutedEventArgs e)
        {
            if (ProjectCheck.IsChecked == true)
            {
                ProjectFileProcessor.Save(Project, _projectDoc);
            }
            if (PackagesConfigCheck.IsChecked == true)
            {
                PackagesConfigFileProcessor.Save(Project, _packagesDoc);
            }
            if (AppConfigCheck.IsChecked == true)
            {
                AppConfigFileProcessor.Save(Project, _appConfigDoc);
            }
            DialogResult = ProjectCheck.IsChecked == true || PackagesConfigCheck.IsChecked == true || AppConfigCheck.IsChecked == true;
        }
    }
}
