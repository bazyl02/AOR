using System;
using System.Linq;
using System.Windows;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace AOR
{
    public partial class MainWindow
    {
        private static IInputDevice device;
        public MainWindow()
        {
            InitializeComponent();
            
            

            /*
            var outputDevice = OutputDevice.GetByIndex(0);
            outputDevice.SendEvent(new NoteOnEvent((SevenBitNumber)60, (SevenBitNumber)45));
            outputDevice.SendEvent(new NoteOffEvent((SevenBitNumber)60, (SevenBitNumber)0)
            {
                DeltaTime = 400
            });
            */
        }

        protected override void OnClosed(EventArgs e)
        {
            device.Dispose();
            base.OnClosed(e);
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            device = InputDevice.GetByName("CASIO USB-MIDI");
            device.EventReceived += OnEventReceived;
            device.StartEventsListening();
            Text1.Text = device.IsListeningForEvents.ToString();
        }
        
        private void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            Text1.Text = "kek";
            Text1.Text = "Event received from " + midiDevice.Name + " at " + DateTime.Now + ": " + e.Event;
        }
    }
}