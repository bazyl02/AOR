using System;
using System.Windows.Controls;
using AOR.Model;
using AOR.ModelView;

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
            string device = (string)e.AddedItems[0];
            Bindings.GetInstance().InputDeviceName = device;
        }

        private void InputComboBox_OnDropDownOpened(object sender, EventArgs e)
        {
            ComboBox.ItemsSource = DeviceController.GetAllInputDeviceNames();
        }
    }
}