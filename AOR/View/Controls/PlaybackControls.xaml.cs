using System.Windows;
using System.Windows.Controls;
using AOR.ModelView;
using Melanchall.DryWetMidi.Multimedia;

namespace AOR.View.Controls
{
    public partial class PlaybackControls : UserControl
    {
        public PlaybackControls()
        {
            InitializeComponent();
        }

        private void PlayFromStart_OnClick(object sender, RoutedEventArgs e)
        {
            Bindings.GetInstance().DeviceController.SimulatedInput?.MoveToStart();
            Bindings.GetInstance().DeviceController.SimulatedInput?.Start();
        }

        private void Play_OnClick(object sender, RoutedEventArgs e)
        {
            Playback simInput = Bindings.GetInstance().DeviceController.SimulatedInput;
            if(simInput == null || simInput.IsRunning) return;
            simInput.Start();
        }

        private void Stop_OnClick(object sender, RoutedEventArgs e)
        {
            Bindings.GetInstance().DeviceController.SimulatedInput?.Stop();
        }
    }
}