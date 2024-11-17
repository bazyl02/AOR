using System;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using AOR.ModelView;
using Melanchall.DryWetMidi.Multimedia;
using Image = System.Windows.Controls.Image;
using RoutedEventArgs = System.Windows.RoutedEventArgs;
using TranslateTransform = System.Windows.Media.TranslateTransform;
using VisualTreeHelper = System.Windows.Media.VisualTreeHelper;
using Window = System.Windows.Window;

namespace AOR
{
    public partial class SheetWindow : Window
    {
        public SheetWindow()
        {
            InitializeComponent();
            DataContext = Bindings.GetInstance();
            SizeChanged += OnSizeChanged;
        }

        public async Task WaitForChange(double currentValue = 0)
        {
            while (Math.Abs(SlidingSheet.ActualWidth - currentValue) < 0.0001f)
            {
                await Task.Delay(25);
            }
            ResetSecondary();
        }
        
        private void OnSizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            ResetSecondary();
        }

        protected override async void OnStateChanged(EventArgs e)
        {
            await WaitForChange(SlidingSheet.ActualWidth);
        }
        
        private void ResetPrimary()
        {
            TranslateTransform transform = new TranslateTransform(0,0);
            MainSheet.RenderTransform = transform;
        }
        
        private void ResetSecondary()
        {
            TranslateTransform transform = new TranslateTransform(ActualWidth * 0.5f + (SlidingSheet.ActualWidth == 0 ? ActualWidth : SlidingSheet.ActualWidth) * 0.5f,0);
            SlidingSheet.RenderTransform = transform;
        }

        public void ResetAll()
        {
            Dispatcher.Invoke(() =>
            {
                ResetPrimary();
                ResetSecondary();
            });
        }
        
        public void MoveSheets(float positionValue)
        {
            double slidingX = (Canvas1.ActualWidth / 2.0 + SlidingSheet.ActualWidth / 2.0) * positionValue;
            double offsetX = slidingX - SlidingSheet.ActualWidth;
            Dispatcher.Invoke(() =>
            {
                if (offsetX < 0)
                {
                    TranslateTransform primaryTransform = new TranslateTransform(offsetX, 0);
                    MainSheet.RenderTransform = primaryTransform;
                }

                TranslateTransform secondaryTransform = new TranslateTransform(slidingX, 0);
                SlidingSheet.RenderTransform = secondaryTransform;
            });
        }
        
        protected override void OnClosed(EventArgs e)
        {
            Bindings.GetInstance().SheetWindow = null;
            if (Bindings.GetInstance().DeviceController.SimulatedInput != null)
            {
                Playback playback = Bindings.GetInstance().DeviceController.SimulatedInput;
                playback.Stop();
                playback.MoveToStart();
            }
        }
        public void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            //MoveTo(0,2000);
        }
    }
}