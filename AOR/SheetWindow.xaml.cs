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
            CorrectSecondary();
        }
        
        private void OnSizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            CorrectSecondary();
        }

        protected override async void OnStateChanged(EventArgs e)
        {
            await WaitForChange(SlidingSheet.ActualWidth);
        }

        private void CorrectSecondary()
        {
            double y = VisualTreeHelper.GetOffset(SlidingSheet).Y;
            TranslateTransform transform = new TranslateTransform(ActualWidth * 0.5f + (SlidingSheet.ActualWidth == 0 ? ActualWidth : SlidingSheet.ActualWidth) * 0.5f,y);
            SlidingSheet.RenderTransform = transform;
        }
        
        public void MoveTo(double newX, int time)
        {
            Image target = SlidingSheet;
            var left = VisualTreeHelper.GetOffset(target).X;
            TranslateTransform transform = new TranslateTransform();
            target.RenderTransform = transform;
            DoubleAnimation animX = new DoubleAnimation(0,newX - left, TimeSpan.FromMilliseconds(time));
            transform.BeginAnimation(TranslateTransform.XProperty, animX);
            transform.Y = 0;
        }

        public void MoveSheets(float positionValue, int time)
        {
            Image main = MainSheet;
            Image sliding = SlidingSheet;
            
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

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            //MoveTo(0,2000);
            CorrectSecondary();
        }
    }
}