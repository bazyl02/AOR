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

        private static Playback _playback;
        
        public MainWindow()
        {
            InitializeComponent();
            _bindings = Bindings.GetInstance();
            DataContext = _bindings;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            _playback = Bindings.GetInstance().SongManager.Songs.Last().GetPlayback(Bindings.GetInstance().DeviceController.OutputDevice);
            _playback?.Start();
        }
    }
}