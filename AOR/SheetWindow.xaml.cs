using System;
using System.Windows;
using System.Windows.Media.Animation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using AOR.ModelView;
using Melanchall.DryWetMidi.Multimedia;
using Image = System.Windows.Controls.Image;
using TranslateTransform = System.Windows.Media.TranslateTransform;
using VisualTreeHelper = System.Windows.Media.VisualTreeHelper;

namespace AOR
{
    public partial class SheetWindow : Window
    {
        public SheetWindow()
        {
            InitializeComponent();
            DataContext = Bindings.GetInstance();
        }

        public static void MoveTo(Image target, double newX, int time)
        {
            var left = VisualTreeHelper.GetOffset(target).X;
            TranslateTransform transform = new TranslateTransform();
            target.RenderTransform = transform;
            DoubleAnimation animX = new DoubleAnimation(0,newX - left, TimeSpan.FromMilliseconds(time));
            transform.BeginAnimation(TranslateTransform.XProperty, animX);
            transform.Y = 0;
            
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

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            MoveTo(Image1,0,2000);
            
        }
    }
}