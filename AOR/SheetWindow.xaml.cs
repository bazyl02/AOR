using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using AOR.ModelView;
using Melanchall.DryWetMidi.Multimedia;
using TranslateTransform = System.Windows.Media.TranslateTransform;
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
            while (Math.Abs(SlidingSheet.ActualWidth - currentValue) < 0.0001f && SlidingSheet.Source != null)
            {
                await Task.Delay(25);
            }
            ResetSecondary();
            ResetPrimary();
        }
        
        private void OnSizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            ResetSecondary();
            ResetPrimary();
        }

        protected override async void OnStateChanged(EventArgs e)
        {
            await WaitForChange(SlidingSheet.ActualWidth);
        }
        
        private void ResetPrimary()
        {
            TranslateTransform transform = new TranslateTransform(0,0);
            MainSheet.RenderTransform = transform;
            TranslateTransform transform2 = new TranslateTransform(0,0);
            MainSheet2.RenderTransform = transform2;
            //-(MainSheet2.ActualWidth == 0 ? ActualWidth : MainSheet2.ActualWidth)
        }
        
        private void ResetSecondary()
        {
            TranslateTransform transform = new TranslateTransform(ActualWidth * 0.5f,0);
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
        
        public void AnimateSheets(double time)
        {
            Dispatcher.Invoke(() =>
            {
                DoubleAnimation animMain =
                    new DoubleAnimation(-MainSheet.ActualWidth, TimeSpan.FromMilliseconds(time));
                DoubleAnimation animMain2 =
                    new DoubleAnimation(-MainSheet2.ActualWidth, TimeSpan.FromMilliseconds(time));
                DoubleAnimation animSliding =
                    new DoubleAnimation(ActualWidth * 0.5f,0, TimeSpan.FromMilliseconds(time));
                TranslateTransform main = new TranslateTransform();
                TranslateTransform main2 = new TranslateTransform();
                TranslateTransform sliding = new TranslateTransform();

                MainSheet.RenderTransform = main;
                MainSheet2.RenderTransform = main2;
                SlidingSheet.RenderTransform = sliding;
                
                main.BeginAnimation(TranslateTransform.XProperty,animMain);
                main2.BeginAnimation(TranslateTransform.XProperty,animMain2);
                sliding.BeginAnimation(TranslateTransform.XProperty,animSliding);
            });
        }

        public void ChangeResizing(bool enable)
        {
            Dispatcher.Invoke(() =>
            {
                ResizeMode = enable ? ResizeMode.CanResize : ResizeMode.NoResize;
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
    }
}