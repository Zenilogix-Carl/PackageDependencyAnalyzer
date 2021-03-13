using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommonServiceLocator;
using Microsoft.Win32;
using PackageDependencyAnalyzer.Properties;
using PackageDependencyAnalyzer.ViewModel;

namespace PackageDependencyAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (MainViewModel) DataContext;
        private bool _mainViewModelWasBusy;

        public MainWindow()
        {
            InitializeComponent();

            var loggerViewModel = ServiceLocator.Current.GetInstance<LoggerViewModel>();
            loggerViewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(LoggerViewModel.Messages))
                {
                    Dispatcher.Invoke(() => Messages.ScrollToEnd());
                }
            };

            ViewModel.PropertyChanged += (sender, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (args.PropertyName == nameof(MainViewModel.IsBusy))
                    {
                        if (ViewModel.IsBusy && !_mainViewModelWasBusy)
                        {
                            Mouse.OverrideCursor = Cursors.Wait;
                        }
                        else if (_mainViewModelWasBusy && !ViewModel.IsBusy)
                        {
                            Mouse.OverrideCursor = null;
                        }

                        _mainViewModelWasBusy = ViewModel.IsBusy;
                    }
                });
            };
        }

        private async void OpenSolution_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Visual Studio Solutions|*.sln",
                DefaultExt = ".sln",
                Multiselect = false,
                CheckFileExists = true
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    await ViewModel.OpenSolution(dlg.FileName);
                    Dispatcher.Invoke(Messages.ScrollToEnd);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message);
                }
            }
        }

        private async void ReloadSolution_OnClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadAsync();
            Dispatcher.Invoke(Messages.ScrollToEnd);
        }

        private void Exit_OnClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void OpenRecent_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem m)
            {
                try
                {
                    await ViewModel.OpenSolution((string)m.DataContext);
                    Dispatcher.Invoke(Messages.ScrollToEnd);
                }
                catch (Exception exception)
                {
                    MessageBox.Show(exception.Message);
                }
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            Settings.Default.Save();
            base.OnClosed(e);
        }

        private void Settings_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new Controls.Settings();
            dlg.ShowDialog();
        }
    }
}
