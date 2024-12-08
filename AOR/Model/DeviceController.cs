using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AOR.ModelView;
using System.Xml.Linq;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;

namespace AOR.Model
{
    public class DeviceController
    {
        private sealed class TimedEventWithTrackChunk : TimedEvent, IMetadata
        {
            public object Metadata { get; set; }
            public TimedEventWithTrackChunk(MidiEvent midiEvent, long time, int trackChunkIndex) : base(midiEvent, time)
            {
                Metadata = trackChunkIndex;
            }
        }
        
        public List<InputDevice> InputDevices = new List<InputDevice>();
        public List<OutputDevice> OutputDevices = new List<OutputDevice>();

        public Dictionary<string, InputDeviceData> InputsOffsets = new Dictionary<string, InputDeviceData>();
        public Dictionary<int, ChannelIDLink> OutputsData = new Dictionary<int, ChannelIDLink>();
        
        private OutputDevice SimulationSoundOutput = null;
        public Playback SimulatedInput = null;
        private short _simulationDivision = 128;
        public const double Speed = 1.0;
        
#if TEST
        public string SimulationName = null;
        public long SimulationTime = 0;
#endif
        
        public void SendToOutput(MidiEventData data)
        {
            OutputDevices[0].SendEvent(data.Event);
        }
        
        public void SetTrackForSimulatedInput(MidiFile track, string name)
        {
            _globalTime = 0;
            _globalRealTime = 0;
            SimulatedInput?.Dispose();
            var timedEvents = track.GetTrackChunks()
                .SelectMany((c, i) => c.GetTimedEvents().Select(e => new TimedEventWithTrackChunk(e.Event, e.Time, i)))
                .OrderBy(e => e.Time);
            var tempoMap = track.GetTempoMap();
            SimulatedInput = new Playback(timedEvents, tempoMap, SimulationSoundOutput);
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
                    Bindings.GetInstance().InputBuffer.BufferSimulatedInput(true,noteOnEvent.NoteNumber);
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
                    Bindings.GetInstance().InputBuffer.BufferSimulatedInput(false,noteOffEvent.NoteNumber);
                    break;
            }
        }
        
        private void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            bool result = InputsOffsets.TryGetValue(midiDevice.Name, out InputDeviceData data);
            if (result)
            {
                bool isNoteOn;
                if (e.Event.EventType == MidiEventType.NoteOn)
                {
                    isNoteOn = true;
                }
                else if(e.Event.EventType == MidiEventType.NoteOff)
                {
                    isNoteOn = false;
                }
                else
                {
                    return;
                }
                NoteEvent noteEvent = (NoteEvent)e.Event;
                if (data.UsesChannels)
                {
                    bool channelResult = data.ChannelsOffsets.TryGetValue(noteEvent.Channel, out int offset);
                    if (channelResult)
                    {
                        Bindings.GetInstance().InputBuffer.BufferUserInput(isNoteOn,(short)(noteEvent.NoteNumber + 128 * offset));
                    }
                }
                else
                {
                    Bindings.GetInstance().InputBuffer.BufferUserInput(isNoteOn,(short)(noteEvent.NoteNumber + 128 * data.Offset));
                }
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
        
        private static List<string> GetAllOutputDeviceNames()
        {
            List<string> output = new List<string>();
            foreach (var device in OutputDevice.GetAll())
            {
                output.Add(device.Name);
            }
            return output;
        }

        public void LoadDeviceConfig(string path)
        {
            XDocument document;
            try
            {
                document = XDocument.Load(path);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            XElement root = document.Root;
            if(root is null) return;
            bool inputMultiChannel = root.Element("inputUsesMultiChannel")?.Value == "Yes";
            bool outputMultiChannel = root.Element("outputUsesMultiChannel")?.Value == "Yes";
            
            InputDevices.Clear();
            OutputDevices.Clear();
            InputsOffsets.Clear();
            OutputsData.Clear();
            
            XElement inputs = root.Element("inputs");
            if(inputs is null || !inputs.HasElements) return;
            var inputDevices = inputs.Elements("input");
            List<string> inputNames = GetAllInputDeviceNames();
            List<string> outputNames = GetAllOutputDeviceNames();
            foreach (XElement inputDevice in inputDevices)
            {
                string inputName = inputDevice.Element("name")?.Value;
                if (inputName != null)
                {
                    if (inputName.Equals("From File"))
                    {
                        Bindings.GetInstance().FromFile = true;
                        XElement soundOutputElement = inputDevice.Element("soundOutput");
                        if(soundOutputElement is null || !outputNames.Contains(soundOutputElement.Value)) return;
                        SimulationSoundOutput = OutputDevice.GetByName(soundOutputElement.Value);
                        break;
                    }
                    Bindings.GetInstance().FromFile = false;
                    if (inputNames.Contains(inputName))
                    {
                        InputDevice inputDev = InputDevice.GetByName(inputName);
                        inputDev.EventReceived += OnEventReceived;
                        InputDevices.Add(inputDev);
                        if (inputMultiChannel)
                        {
                            XElement channelsElement = inputDevice.Element("channels");
                            if(channelsElement == null) return;
                            var channels = channelsElement.Elements("channel");
                            InputDeviceData newInputDevice = new InputDeviceData(inputName, 0, true);
                            InputsOffsets.Add(inputName,newInputDevice);
                            foreach (XElement channel in channels)
                            {
                                XElement idElement = channel.Element("id");
                                if(idElement == null) return;
                                int channelId = int.Parse(idElement.Value); 
                                XElement offsetElement = inputDevice.Element("offset");
                                if(offsetElement is null) return;
                                int offset = int.Parse(offsetElement.Value); 
                                newInputDevice.ChannelsOffsets.Add(channelId,offset);
                            }
                        }
                        else
                        {
                            XElement offsetElement = inputDevice.Element("offset");
                            if(offsetElement is null) return;
                            int offset = int.Parse(offsetElement.Value); 
                            InputsOffsets.Add(inputName,new InputDeviceData(inputName,offset,false));
                        }
                    }
                    else
                    {
                        throw new InvalidDataException("Couldn't find the device: " + inputName);
                    }
                }
            }
            
            XElement outputs = root.Element("outputs");
            if(outputs is null || !outputs.HasElements) return;
            var outputDevices = outputs.Elements("output");
            var xElements = outputDevices.ToList();
            if(xElements.Count == 0) return;
            for (int i = 0; i < xElements.Count; i++)
            {
                OutputDevices.Add(null);
            }
            
            foreach (XElement outputDevice in xElements)
            {
                string outputName = outputDevice.Element("name")?.Value;
                Console.WriteLine(outputName);
                if (outputName != null && outputNames.Contains(outputName))
                {
                    OutputDevice outputDev = OutputDevice.GetByName(outputName);
                    outputDev.EventSent += OnEventSent;
                    
                    XElement deviceIdElement = outputDevice.Element("deviceID");
                    if(deviceIdElement is null) return;
                    int id = int.Parse(deviceIdElement.Value);
                    OutputDevices[id] = outputDev;
                    if (outputMultiChannel)
                    {
                        XElement channelsElement = outputDevice.Element("channels");
                        if(channelsElement == null) return;
                        var channels = channelsElement.Elements("channel");
                        foreach (XElement channel in channels)
                        {
                            XElement globalIdElement = channel.Element("globalID");
                            if(globalIdElement is null) return;
                            int ID = int.Parse(globalIdElement.Value);
                            XElement channelIdElement = channel.Element("channelID");
                            if(channelIdElement is null) return;
                            int channelId = int.Parse(channelIdElement.Value);
                            OutputsData.Add(ID,new ChannelIDLink(true,id,channelId));
                        }
                    }
                    else
                    {
                        XElement globalIdElement = outputDevice.Element("globalID");
                        if(globalIdElement is null) return;
                        int ID = int.Parse(globalIdElement.Value);
                        OutputsData.Add(ID,new ChannelIDLink(false,id,-1));
                    }
                }
            }
        }
    }
}