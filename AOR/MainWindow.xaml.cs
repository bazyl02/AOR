using System;
using System.IO;
using System.Windows;
using AOR.ModelView;
using AOR.Model;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace AOR
{
    public partial class MainWindow
    {
        private DeviceController DeviceController;

        private Bindings _bindings;

        private static Playback _playback;
        
        public MainWindow()
        {
            InitializeComponent();
            _bindings = Bindings.GetInstance();
            DataContext = _bindings;
            _bindings.DeviceController = new DeviceController();
            StreamWriter raportFile = new StreamWriter("../../Data/Raport1.txt");
            MidiFile file = MidiFile.Read("../../Data/Megalovania.mid");
            var chunks = file.Chunks;
            Console.WriteLine(chunks.Count);
            foreach (TrackChunk chunk in chunks)
            {
                raportFile.WriteLine("Chunk size: " + chunk.Events.Count);
                raportFile.WriteLine("Events: ");
                foreach (MidiEvent event1 in chunk.Events)
                {
                    raportFile.WriteLine(event1.ToString() + " | Time: " + event1.DeltaTime);
                }
                raportFile.WriteLine("------------------------------------------------");
            }
            raportFile.Close();
            
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            MidiFile file = MidiFile.Read("../../Data/Megalovania.mid");
            _playback = file.GetPlayback(Bindings.GetInstance().DeviceController.OutputDevice);
            _playback.Start();
            
        }
    }
}