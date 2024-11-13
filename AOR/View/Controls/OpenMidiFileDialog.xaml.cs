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

        private async void OpenFile_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = true;
            fileDialog.Filter = "Song package | *.zip";
            fileDialog.Title = "Select a song package to open";
            bool? success = fileDialog.ShowDialog();
            if (success == true)
            {
                foreach (var fileName in fileDialog.FileNames)
                {
                    await Bindings.GetInstance().SongManager.LoadSong(fileName);
                }
                //string path = fileDialog.FileName;
                Bindings.GetInstance().SongList.UpdateSongList(Bindings.GetInstance().SongManager.Pieces); 
            }
        }
    }
}