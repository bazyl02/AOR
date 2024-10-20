using System;
using System.Windows.Controls;
using AOR.Model;
using AOR.ModelView;
using Melanchall.DryWetMidi.Core;

namespace AOR.View.Controls
{
    public partial class SimulatedInputFileDropDown : UserControl
    {
        public SimulatedInputFileDropDown()
        {
            InitializeComponent();
        }

        private void ComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SongManager.PieceData piece = (SongManager.PieceData)e.AddedItems[0];
            Console.WriteLine(piece.Name);
            //Bindings.GetInstance().InputDeviceName = device;
        }

        private void ComboBox_OnDropDownOpened(object sender, EventArgs e)
        {
            ComboBox.ItemsSource = Bindings.GetInstance().SongManager.Pieces;
        }
    }
}