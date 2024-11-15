using System;
using System.Windows;
using AOR.ModelView;
using Melanchall.DryWetMidi.Multimedia;

namespace AOR
{
    public partial class SheetWindow : Window
    {
        public SheetWindow()
        {
            InitializeComponent();
            DataContext = Bindings.GetInstance();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (Bindings.GetInstance().DeviceController.SimulatedInput != null)
            {
                Playback playback = Bindings.GetInstance().DeviceController.SimulatedInput;
                playback.Stop();
                playback.MoveToStart();
            }
        }
    }
}