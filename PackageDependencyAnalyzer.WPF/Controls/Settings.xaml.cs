﻿using System;
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
using System.Windows.Shapes;
using Microsoft.Win32;
using PackageDependencyAnalyzer.ViewModel;

namespace PackageDependencyAnalyzer.Controls
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        private SettingsViewModel ViewModel => (SettingsViewModel) DataContext;

        public Settings()
        {
            InitializeComponent();
        }

        private void Browse_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Executable Files|*.exe",
                DefaultExt = ".exe",
                Multiselect = false,
                CheckFileExists = true,
                FileName = ViewModel.FileEditorCommand
            };
            if (dlg.ShowDialog() == true)
            {
                ViewModel.FileEditorCommand = dlg.FileName;
            }
        }
    }
}