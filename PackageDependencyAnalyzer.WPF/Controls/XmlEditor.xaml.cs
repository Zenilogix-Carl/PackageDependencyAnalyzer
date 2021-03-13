using System.IO;
using System.Windows;

namespace PackageDependencyAnalyzer.Controls
{
    /// <summary>
    /// Interaction logic for XmlEditor.xaml
    /// </summary>
    public partial class XmlEditor : Window
    {
        public string FileName { get; set; }
        public int InitialLineNumber { get; set; }
        public string SelectMatchingText { get; set; }

        public XmlEditor()
        {
            InitializeComponent();

            Loaded += (sender, args) =>
            {
                Title = FileName;
                Editor.Text = File.ReadAllText(FileName);
                Editor.ScrollToLine(InitialLineNumber);
                var pos = Editor.Text.IndexOf(SelectMatchingText);
                if (pos >= 0)
                {
                    Editor.Select(pos, SelectMatchingText.Length);
                }
            };
        }

        private void SaveAndClose_OnClick(object sender, RoutedEventArgs e)
        {
            File.WriteAllText(FileName, Editor.Text);
            DialogResult = true;
        }
    }
}
