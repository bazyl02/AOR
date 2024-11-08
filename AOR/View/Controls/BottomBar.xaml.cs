using System;
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

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            SheetWindow win2 = new SheetWindow();
            win2.Show();
            Bindings.GetInstance().ProcessSelectedPiece();
            Bindings.GetInstance().InputBuffer.Clear();
            Bindings.GetInstance().Algorithm = new Algorithm();
        }
    }
}