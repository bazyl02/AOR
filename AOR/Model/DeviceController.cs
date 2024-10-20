using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Multimedia;

namespace AOR.Model
{
    public class DeviceController
    {
        public InputDevice InputDevice = null;
        public OutputDevice OutputDevice = null;

        public Playback SimulatedInput = null;

        public bool FromFile = false;

        public void SetInputDevice(string name)
        {
            if (InputDevice != null)
            {
                InputDevice.Dispose();
            }
            if (name.Equals("From File"))
            {
                FromFile = true;
                //SetSimulatedInput();
                return;
            }
            FromFile = false;
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

        public void SetSimulatedInput(Playback playback)
        {
            SimulatedInput?.Stop();
            SimulatedInput?.Dispose();

            SimulatedInput = playback;
            SimulatedInput.EventPlayed += OnEventPlayed;
            SimulatedInput?.Start();
        }
        
        
        private void OnEventPlayed(object sender, MidiEventPlayedEventArgs args)
        {
            Playback playback = (Playback)sender;
            Console.WriteLine("Event received from Midi File at " + DateTime.Now + ": " + args.Event);
        }
        private void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            Console.WriteLine("Event received from " + midiDevice.Name + " at " + DateTime.Now + ": " + e.Event);
        }

        private void OnEventSent(object sender, MidiEventSentEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            //Console.WriteLine("Event sent to " + midiDevice.Name + " at " + DateTime.Now + ": " + e.Event);
        }

        public static List<string> GetAllInputDeviceNames()
        {
            List<string> output = new List<string>();
            foreach (var device in InputDevice.GetAll())
            {
                output.Add(device.Name);
            }
            output.Add("From File");
            return output;
        }
        
    }
}