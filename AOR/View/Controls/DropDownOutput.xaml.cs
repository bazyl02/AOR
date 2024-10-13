using System;
using System.Windows.Controls;
using AOR.ModelView;
using Melanchall.DryWetMidi.Multimedia;

namespace AOR.View.Controls
{
    public partial class DropDownOutput : UserControl
    {
        public DropDownOutput()
        {
            InitializeComponent();
        }
        
        private void OutputComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OutputDevice device = (OutputDevice)e.AddedItems[0];
            Bindings.GetInstance().OutputDeviceName = device.Name;
        }

        private void OutputComboBox_OnDropDownOpened(object sender, EventArgs e)
        {
            ComboBox.ItemsSource = OutputDevice.GetAll();
        }
    }
}