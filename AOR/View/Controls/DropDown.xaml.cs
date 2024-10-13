using System;
using System.Windows.Controls;
using AOR.ModelView;
using Melanchall.DryWetMidi.Multimedia;

namespace AOR.View.Controls
{
    public partial class DropDown : UserControl
    {
        public DropDown()
        {
            InitializeComponent();
        }

        private void InputComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InputDevice device = (InputDevice)e.AddedItems[0];
            Bindings.GetInstance().InputDeviceName = device.Name;
        }

        private void InputComboBox_OnDropDownOpened(object sender, EventArgs e)
        {
            ComboBox.ItemsSource = InputDevice.GetAll();
        }
    }
}