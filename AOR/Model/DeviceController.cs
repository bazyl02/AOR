using System;
using System.Collections.Generic;
using AOR.ModelView;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;

namespace AOR.Model
{
    public class DeviceController
    {
        public InputDevice InputDevice = null;
        public OutputDevice OutputDevice = null;

        public Playback SimulatedInput = null;
        private short _simulationDivision = 128;
        public const double Speed = 1.0;
        
#if TEST
        public string SimulationName = null;
        public long SimulationTime = 0;
#endif
        
        public void SetInputDevice(string name)
        {
            if (InputDevice != null)
            {
                InputDevice.Dispose();
            }
            if (name.Equals("From File"))
            {
                Bindings.GetInstance().FromFile = true;
                //SetSimulatedInput();
                return;
            }
            Bindings.GetInstance().FromFile = false;
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
        
        public void SetTrackForSimulatedInput(MidiFile track, string name)
        {
            _globalTime = 0;
            _globalRealTime = 0;
            SimulatedInput?.Dispose();
            SimulatedInput = OutputDevice != null ? track.GetPlayback(OutputDevice) : track.GetPlayback();
            SimulationName = name;
            _simulationDivision = ((TicksPerQuarterNoteTimeDivision)track.TimeDivision).TicksPerQuarterNote;
            SimulatedInput.Speed = Speed;
            SimulatedInput.EventPlayed += OnEventPlayed;
        }
        
        private long _globalTime = 0;
        private long _globalRealTime = 0;
        private void OnEventPlayed(object sender, MidiEventPlayedEventArgs args)
        {
            Playback playback = (Playback)sender;
            MidiEventType type = args.Event.EventType;
            switch (type)
            {
                case MidiEventType.NoteOn:
                    NoteOnEvent noteOnEvent = (NoteOnEvent)args.Event;
                    _globalTime += noteOnEvent.DeltaTime;
                    long tempoOn = playback.TempoMap.GetTempoAtTime(new MidiTimeSpan(_globalTime)).MicrosecondsPerQuarterNote;
                    double dividerOn = (tempoOn / (_simulationDivision * 1.0d)) / InputBuffer.TickResolution * 1.0d;
                    long timeOn = (long)Math.Round(noteOnEvent.DeltaTime * dividerOn);
                    _globalRealTime += timeOn;
                    Bindings.GetInstance().InputBuffer.BufferSimulatedInput(true,noteOnEvent.NoteNumber, noteOnEvent.DeltaTime);
                    break;
                case MidiEventType.NoteOff:
                    NoteOffEvent noteOffEvent = (NoteOffEvent)args.Event;
                    _globalTime += noteOffEvent.DeltaTime;
                    long tempo = playback.TempoMap.GetTempoAtTime(new MidiTimeSpan(_globalTime)).MicrosecondsPerQuarterNote;
                    double divider = (tempo / (_simulationDivision * 1.0d)) / InputBuffer.TickResolution * 1.0d;
                    long time = (long)Math.Round(noteOffEvent.DeltaTime * divider);
                    _globalRealTime += time;
                    Console.WriteLine(@"Simulated input time: " + _globalRealTime);
#if DUMP
                    Bindings.GetInstance().Report.WriteLine("Simulated input time: " + _globalRealTime);
#endif
#if TEST
                    SimulationTime = _globalRealTime;
#endif
                    Bindings.GetInstance().InputBuffer.BufferSimulatedInput(false,noteOffEvent.NoteNumber, noteOffEvent.DeltaTime);
                    break;
            }
        }
        
        private void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            MidiEventType type = e.Event.EventType;
            switch (type)
            {
                case MidiEventType.NoteOn:
                    NoteOnEvent noteOnEvent = (NoteOnEvent)e.Event;
                    //Console.WriteLine("Note On event received from " + midiDevice.Name + " at " + DateTime.Now + " tone: " + noteOnEvent.NoteNumber);
                    Bindings.GetInstance().InputBuffer.BufferUserInput(true,noteOnEvent.NoteNumber);
                    break;
                case MidiEventType.NoteOff:
                    NoteOffEvent noteOffEvent = (NoteOffEvent)e.Event;
                    //Console.WriteLine("Note Off event received from " + midiDevice.Name + " at " + DateTime.Now + " tone: " + noteOffEvent.NoteNumber);
                    Bindings.GetInstance().InputBuffer.BufferUserInput(false,noteOffEvent.NoteNumber);
                    break;
            }
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