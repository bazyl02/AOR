using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace AOR.View.Controls
{
    public partial class ConfigFileDialog : UserControl
    {
        public ConfigFileDialog()
        {
            InitializeComponent();
        }

        private void LoadConfig_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Filter = "Config file | *.xml",
                Title = "Select a configuration file to load"
            };
            bool? success = fileDialog.ShowDialog();
            if (success == true)
            {
                Console.WriteLine(fileDialog.FileName);
            }
        }
    }
}