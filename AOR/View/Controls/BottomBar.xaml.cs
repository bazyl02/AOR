﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using AOR.Model;
using AOR.ModelView;

namespace AOR.View.Controls
{
    public partial class BottomBar : UserControl
    {
        public BottomBar()
        {
            InitializeComponent();
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (Bindings.GetInstance().SelectedPiece == null ||
                Bindings.GetInstance().DeviceController.OutputDevice == null || (Bindings.GetInstance().FromFile
                    ? Bindings.GetInstance().DeviceController.SimulatedInput == null
                    : Bindings.GetInstance().DeviceController.InputDevice == null))
            {
                MessageBox.Show("Either output device, selected piece or " + (Bindings.GetInstance().FromFile ? "simulation input" : "input device") + " is missing!","Error!",MessageBoxButton.OK,MessageBoxImage.Error);
                return;
            }
            SheetWindow win2 = new SheetWindow();
            Bindings.GetInstance().ProcessSelectedPiece(); 
            Bindings.GetInstance().SheetWindow = win2;
            win2.Show();
            await Bindings.GetInstance().SheetWindow.WaitForChange();
            Bindings.GetInstance().InputBuffer.Clear();
            Bindings.GetInstance().Algorithm = new Algorithm();
            
            var raportStream = Bindings.GetInstance().Report;
            raportStream?.Close();
            string docPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            Console.WriteLine(DateTime.Now.ToLongDateString());
            raportStream = new StreamWriter(docPath + "/" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + "_" + Bindings.GetInstance().SelectedPiece.SongName +"_test.txt");
            Bindings.GetInstance().Report = raportStream;
            Bindings.GetInstance().PieceBuffer.DumpMelodyBufferToReport();
        }
    }
}