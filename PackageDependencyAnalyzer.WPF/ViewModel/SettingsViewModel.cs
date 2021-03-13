using GalaSoft.MvvmLight;

namespace PackageDependencyAnalyzer.ViewModel
{
    public class SettingsViewModel : ViewModelBase
    {
        public string FileEditorCommand
        {
            get => Properties.Settings.Default.TextEditor;
            set
            {
                Properties.Settings.Default.TextEditor = value;
                RaisePropertyChanged();
            }
        }
    }
}
