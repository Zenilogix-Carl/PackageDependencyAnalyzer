using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PackageDependencyAnalyzer.ViewModel;

namespace PackageDependencyAnalyzer.Controls
{
    /// <summary>
    /// Interaction logic for NamespaceTreeControl.xaml
    /// </summary>
    public partial class NamespaceTreeControl : UserControl
    {
        private SolutionViewModel ViewModel => (SolutionViewModel) DataContext;

        public NamespaceTreeControl()
        {
            InitializeComponent();
        }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            ViewModel.SelectedNamespace = (NamespaceViewModel) e.NewValue;
        }
    }
}
