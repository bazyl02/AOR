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
            SheetWindow win2 = new SheetWindow();
            Bindings.GetInstance().ProcessSelectedPiece(); 
            Bindings.GetInstance().SheetWindow = win2;
            win2.Show();
            await Bindings.GetInstance().SheetWindow.WaitForChange();
            Bindings.GetInstance().InputBuffer.Clear();
            Bindings.GetInstance().Algorithm = new Algorithm();
            
        }
    }
}