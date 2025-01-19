using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using AOR.Model;
using AOR.ModelView;

namespace AOR.View.Controls
{
    public partial class StartButton : UserControl
    {
        public StartButton()
        {
            InitializeComponent();
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (Bindings.GetInstance().SelectedPiece == null ||
                !Bindings.GetInstance().DeviceController.ConfigLoaded || (Bindings.GetInstance().FromFile
                    ? Bindings.GetInstance().DeviceController.SimulatedInput == null
                    : Bindings.GetInstance().DeviceController.InputCount == 0))
            {
                MessageBox.Show("Either instrument config or selected piece is missing!","Error!",MessageBoxButton.OK,MessageBoxImage.Error);
                return;
            }

            if (Bindings.GetInstance().SheetWindow != null)
            {
                Bindings.GetInstance().SheetWindow.Close();
                Bindings.GetInstance().SheetWindow = null;
            }
            SheetWindow win2 = new SheetWindow();
            Bindings.GetInstance().InputBuffer.Clear();
            Bindings.GetInstance().ProcessSelectedPiece(); 
            Bindings.GetInstance().SheetWindow = win2;
            win2.Show();
            await Bindings.GetInstance().SheetWindow.WaitForChange();
            Bindings.GetInstance().Algorithm = new Algorithm();
            
#if DUMP
            var raportStream = Bindings.GetInstance().Report;
            raportStream?.Close();
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            raportStream = new StreamWriter(docPath + "/" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_" + Bindings.GetInstance().SelectedPiece.SongName +"_dump.txt");
            Bindings.GetInstance().Report = raportStream;
            Bindings.GetInstance().PieceBuffer.DumpMelodyBufferToReport();
#endif
            
#if TEST
            var testStream = Bindings.GetInstance().TestResult;
            testStream?.Close();
            string path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            testStream = new StreamWriter(path + "/" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_" + Bindings.GetInstance().DeviceController.SimulationName + "_Test.txt");
            Bindings.GetInstance().TestResult = testStream;
#endif
        }
    }
}