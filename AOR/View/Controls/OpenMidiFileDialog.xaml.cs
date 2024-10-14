using System.Windows;
using System.Windows.Controls;
using AOR.ModelView;
using Microsoft.Win32;

namespace AOR.View.Controls
{
    public partial class OpenMidiFileDialog : UserControl
    {
        public OpenMidiFileDialog()
        {
            InitializeComponent();
        }

        private void OpenFile_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "MIDI file | *.mid";
            fileDialog.Title = "Select a MIDI file to open";
            bool? success = fileDialog.ShowDialog();
            if (success == true)
            {
                string path = fileDialog.FileName;
                Bindings.GetInstance().SongManager.LoadSong(path);
            }
        }
    }
}