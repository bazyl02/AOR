using System;
using System.Linq;
using System.Windows;
using AOR.ModelView;
using AOR.Model;
using Melanchall.DryWetMidi.Multimedia;

namespace AOR
{
    public partial class MainWindow
    {
        private DeviceController DeviceController;

        private Bindings _bindings;
        
        public MainWindow()
        {
            InitializeComponent();
            _bindings = Bindings.GetInstance();
            DataContext = _bindings;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            //Bindings.GetInstance().DeviceController.SetSimulatedInput(Bindings.GetInstance().SongManager.Pieces.Last().MidiFile.GetPlayback(Bindings.GetInstance().DeviceController.OutputDevice));
            foreach (var song in Bindings.GetInstance().SongManager.Pieces)
            {
                Console.WriteLine(song.Name);
            }
            
        }
        
    }
}