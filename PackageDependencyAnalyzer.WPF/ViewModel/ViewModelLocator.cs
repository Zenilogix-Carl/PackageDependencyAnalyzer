/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:PackageDependencyAnalyzer"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=ViewModelName}"

  You can also use Blend to do all this with the tool's support.
  See http://www.galasoft.ch/mvvm
*/

using CommonServiceLocator;
using GalaSoft.MvvmLight.Ioc;

namespace PackageDependencyAnalyzer.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        /// <summary>
        /// Initializes a new instance of the ViewModelLocator class.
        /// </summary>
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            ////if (ViewModelBase.IsInDesignModeStatic)
            ////{
            ////    // Create design time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DesignDataService>();
            ////}
            ////else
            ////{
            ////    // Create run time view services and models
            ////    SimpleIoc.Default.Register<IDataService, DataService>();
            ////}

            SimpleIoc.Default.Register<MainViewModel>();
            SimpleIoc.Default.Register<PackageCacheViewModel>();
            SimpleIoc.Default.Register<SolutionViewModel>();
            SimpleIoc.Default.Register<LoggerViewModel>();
            SimpleIoc.Default.Register<SettingsViewModel>();
        }

        public MainViewModel Main => ServiceLocator.Current.GetInstance<MainViewModel>();
        public PackageCacheViewModel PackageCacheViewModel => ServiceLocator.Current.GetInstance<PackageCacheViewModel>();
        public SolutionViewModel SolutionViewModel => ServiceLocator.Current.GetInstance<SolutionViewModel>();
        public LoggerViewModel LoggerViewModel => ServiceLocator.Current.GetInstance<LoggerViewModel>();
        public SettingsViewModel SettingsViewModel => ServiceLocator.Current.GetInstance<SettingsViewModel>();

        public static void Cleanup()
        {
            // TODO Clear the ViewModels
        }
    }
}