using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PackageDependencyAnalysis.Model;
using PackageDependencyAnalyzer.ViewModel;

namespace PackageDependencyAnalyzer.Controls
{
    /// <summary>
    /// Interaction logic for SolutionControl.xaml
    /// </summary>
    public partial class SolutionControl : UserControl
    {
        private ViewModelLocator ViewModel => (ViewModelLocator) DataContext;

        public SolutionControl()
        {
            InitializeComponent();
        }

        private void AllProjects_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AllProjects.ScrollIntoView(ViewModel.SolutionViewModel.ProjectViewModel);
        }

        private void AllClear_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            All.Text = string.Empty;
        }

        private void Search_OnDropDownClosed(object sender, System.Windows.RoutedPropertyChangedEventArgs<bool> e)
        {
            var autoCompleteBox = ((AutoCompleteBox)sender);
            var key = autoCompleteBox.Text;
            var project = ViewModel.SolutionViewModel.Projects.FirstOrDefault(s => s.Name == key);
            if (project != null)
            {
                Dispatcher.Invoke(() =>
                {
                    AllProjects.SelectedItem = project;
                    AllProjects.ScrollIntoView(project);
                    autoCompleteBox.Text = string.Empty;
                });
            }
        }

        private void All_OnPopulating(object sender, PopulatingEventArgs e)
        {
            ((AutoCompleteBox)sender).ItemsSource = ViewModel.SolutionViewModel?.Projects?.Select(s => s.Name);
        }

        private void NamespaceProjects_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView lv && e.AddedItems.Count > 0)
            {
                lv.ScrollIntoView(e.AddedItems[0]);
            }
        }

        private void ProjectEdit_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem m && m.DataContext is IProject p)
            {
                var fileCommand = Properties.Settings.Default.TextEditor;
                if (string.IsNullOrEmpty(fileCommand))
                {
                    Process.Start(p.AbsolutePath);
                }
                else
                {
                    Process.Start(fileCommand, p.AbsolutePath);
                }
            }
        }

        private void AppConfigEdit_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem m && m.DataContext is ProjectViewModel p)
            {
                var fileCommand = Properties.Settings.Default.TextEditor;
                if (string.IsNullOrEmpty(fileCommand))
                {
                    Process.Start(Path.Combine(p.Folder, "app.config"));
                }
                else
                {
                    Process.Start(fileCommand, Path.Combine(p.Folder, "app.config"));
                }
            }
        }

        private void PackagesConfigEdit_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem m && m.DataContext is ProjectViewModel p)
            {
                var fileCommand = Properties.Settings.Default.TextEditor;
                if (string.IsNullOrEmpty(fileCommand))
                {
                    Process.Start(Path.Combine(p.Folder, "packages.config"));
                }
                else
                {
                    Process.Start(fileCommand, Path.Combine(p.Folder, "packages.config"));
                }
            }
        }
    }
}
