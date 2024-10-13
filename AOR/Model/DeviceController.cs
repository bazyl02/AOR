using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Multimedia;

namespace AOR.Model
{
    public class DeviceController
    {
        public InputDevice InputDevice = null;
        public OutputDevice OutputDevice = null;

        public void SetInputDevice(string name)
        {
            if (InputDevice != null)
            {
                InputDevice.Dispose();
            }
            InputDevice = InputDevice.GetByName(name);
            InputDevice.StartEventsListening();
            InputDevice.EventReceived += OnEventReceived;
        }
        
        public void SetOutputDevice(string name)
        {
            if (OutputDevice != null)
            {
                OutputDevice.Dispose();
            }
            OutputDevice = OutputDevice.GetByName(name);
            
            OutputDevice.EventSent += OnEventSent;
        }
        
        private void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            Console.WriteLine("Event received from " + midiDevice.Name + " at " + DateTime.Now + ": " + e.Event);
        }

        private void OnEventSent(object sender, MidiEventSentEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            Console.WriteLine("Event sent to " + midiDevice.Name + " at " + DateTime.Now + ": " + e.Event);
        }

        public static List<string> GetAllDevicesNames()
        {
            List<string> output = new List<string>();
            foreach (var device in InputDevice.GetAll())
            {
                output.Add(device.Name);
            }
            return output;
        }
        
    }
}