using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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
        
        private readonly List<InputDevice> _inputDevices = new List<InputDevice>();
        private readonly List<OutputDevice> _outputDevices = new List<OutputDevice>();

        private readonly Dictionary<string, InputDeviceData> _inputsOffsets = new Dictionary<string, InputDeviceData>();
        private readonly Dictionary<int, ChannelIdLink> _outputsData = new Dictionary<int, ChannelIdLink>();

        private readonly Dictionary<int, PieceBuffer.TrackData> _simulationOffsets =
            new Dictionary<int, PieceBuffer.TrackData>();
        
        private OutputDevice _simulationSoundOutput ;
        public Playback SimulatedInput;
        private short _simulationDivision = 128;
        private const double Speed = 1.0f;

        public int InputCount => _inputDevices.Count;

#if TEST
        public string SimulationName;
        public long SimulationTime;
#endif
        
        public void SendToOutput(MidiEventData data)
        {
            //ProgramChangeEvent pr = new ProgramChangeEvent(new SevenBitNumber(3));
            //pr.Channel = new FourBitNumber(15);
            //_outputDevices[0].SendEvent(pr);
            //return;
            if (!_outputsData.TryGetValue(data.GlobalId, out ChannelIdLink encoding)) return;
            switch (data.Event.EventType)
            {
                case MidiEventType.NoteOn:
                case MidiEventType.NoteOff:
                {
                    NoteEvent noteEvent = (NoteEvent)data.Event;
                    if(encoding.UsesChannel)noteEvent.Channel = new FourBitNumber((byte)encoding.ChannelId);
                    _outputDevices[encoding.DeviceId].SendEvent(noteEvent);
                    break;
                }
                case MidiEventType.ProgramChange:
                {
                    ProgramChangeEvent programChangeEvent = (ProgramChangeEvent)data.Event;
                    if(encoding.UsesChannel)programChangeEvent.Channel = new FourBitNumber((byte)encoding.ChannelId);
                    _outputDevices[encoding.DeviceId].SendEvent(programChangeEvent);
                    break;
                }
                case MidiEventType.NormalSysEx:
                {
                    _outputDevices[encoding.DeviceId].SendEvent(data.Event);
                    break;
                }
            }
        }
        
        public void SetTrackForSimulatedInput(MidiFile track,XDocument config, string name)
        {
            _globalTime = 0;
            _globalRealTime = 0;
            SimulatedInput?.Dispose();
            var timedEvents = track.GetTrackChunks()
                .SelectMany((c, i) => c.GetTimedEvents().Select(e => new TimedEventWithTrackChunk(e.Event, e.Time, i)))
                .OrderBy(e => e.Time);
            var tempoMap = track.GetTempoMap();
            SimulatedInput = new Playback(timedEvents, tempoMap, _simulationSoundOutput);
#if TEST
            SimulationName = name;
#endif
            _simulationDivision = ((TicksPerQuarterNoteTimeDivision)track.TimeDivision).TicksPerQuarterNote;
            SimulatedInput.Speed = Speed;
            SimulatedInput.EventPlayed += OnEventPlayed;
            _simulationOffsets.Clear();
            SetupSimulationOffsets(config);
        }

        private void SetupSimulationOffsets(XDocument config)
        {
            var tracks = config.Root?.Element("tracks")?.Elements("track");
            if (tracks is null) throw new ArgumentException("No 'tracks' tag in simulation config file!");
            foreach (XElement track in tracks)
            {
                XElement trackIdElement = track.Element("trackID");
                if (trackIdElement is null || !int.TryParse(trackIdElement.Value,out int trackId))throw new ArgumentException("At least one of the simulation tracks does not have 'trackID' tag");
                XElement usesChannelsElement = track.Element("usesMultiChannel");
                if (usesChannelsElement is null) throw new ArgumentException("At least one of the simulation tracks does not have 'usesMultiChannel' tag");
                bool usesChannels = usesChannelsElement.Value == "True";
                PieceBuffer.TrackData trackData = null;
                if (usesChannels)
                {
                    trackData = new PieceBuffer.TrackData();
                    var channels = track.Element("channels")?.Elements("channel");
                    if (channels is null) throw new ArgumentException("At least one of the simulation tracks does not have 'channels' tag even thought track is marked as multichannel one");
                    foreach (XElement channel in channels)
                    {
                        PieceBuffer.ChannelData channelData = null;
                        XElement channelIdElement = channel.Element("channelID");
                        if (channelIdElement is null || !int.TryParse(channelIdElement.Value,out int channelId))
                            throw new ArgumentException("At least one of the channels in simulation track no."+ trackId +" does not have proper 'channelID' tag");
                        XElement usageElement = channel.Element("use");
                        if (usageElement is null || !Enum.TryParse(usageElement.Value,out PieceBuffer.Usage channelUsage)) throw new ArgumentException("At least one of the channels in simulation track no." + trackId +" does not have 'use' tag even thought track is marked as multichannel one");
                        if (channelUsage == PieceBuffer.Usage.Melody)
                        {
                            XElement dataElement = channel.Element("offset");
                            if (dataElement is null || !int.TryParse(dataElement.Value,out int dt)) throw new ArgumentException("At least one of channels in simulation track no." + trackId +" does not have 'offset' tag");
                            channelData = new PieceBuffer.ChannelData(channelUsage, dt);
                            trackData.Channels.Add(channelId,channelData);
                            _simulationOffsets.Add(trackId,trackData);
                        }
                    }
                }
                else
                {
                    XElement usageElement = track.Element("use");
                    if (usageElement is null || !Enum.TryParse(usageElement.Value,out PieceBuffer.Usage trackUsage)) throw new ArgumentException("Simulation track no." + trackId +" does not have 'use' tag even thought track is not marked as multichannel one");
                    if (trackUsage == PieceBuffer.Usage.Melody)
                    {
                        XElement dataElement = track.Element("offset");
                        if (dataElement is null || !int.TryParse(dataElement.Value,out int dt)) throw new ArgumentException("At least one of simulation tracks does not have 'offset' tag");
                        trackData = new PieceBuffer.TrackData(trackUsage, dt);
                        _simulationOffsets.Add(trackId,trackData);
                    } 
                }
            }
        }
        
        private long _globalTime;
        private long _globalRealTime;
        private void OnEventPlayed(object sender, MidiEventPlayedEventArgs args)
        {
            Playback playback = (Playback)sender;
            MidiEventType type = args.Event.EventType;
            int trackId = (int)args.Metadata;
            if(!_simulationOffsets.TryGetValue(trackId,out PieceBuffer.TrackData trackData)) return;
            int offset = 0;
            if (!trackData.UsesChannels)
            {
                offset = trackData.Data;
            }
            switch (type)
            {
                case MidiEventType.NoteOn:
                    NoteOnEvent noteOnEvent = (NoteOnEvent)args.Event;
                    _globalTime += noteOnEvent.DeltaTime;
                    long tempoOn = playback.TempoMap.GetTempoAtTime(new MidiTimeSpan(_globalTime)).MicrosecondsPerQuarterNote;
                    double dividerOn = (tempoOn / (_simulationDivision * 1.0d)) / InputBuffer.TickResolution * 1.0d;
                    long timeOn = (long)Math.Round(noteOnEvent.DeltaTime * dividerOn);
                    _globalRealTime += timeOn;
                    if (trackData.UsesChannels && trackData.Channels.TryGetValue(noteOnEvent.Channel,out PieceBuffer.ChannelData channelData))
                    {
                        offset = channelData.Data;
                    }
                    Bindings.GetInstance().InputBuffer.BufferSimulatedInput(true,(short)(noteOnEvent.NoteNumber + 128 * offset));
                    break;
                case MidiEventType.NoteOff:
                    NoteOffEvent noteOffEvent = (NoteOffEvent)args.Event;
                    _globalTime += noteOffEvent.DeltaTime;
                    long tempo = playback.TempoMap.GetTempoAtTime(new MidiTimeSpan(_globalTime)).MicrosecondsPerQuarterNote;
                    double divider = (tempo / (_simulationDivision * 1.0d)) / InputBuffer.TickResolution * 1.0d;
                    long time = (long)Math.Round(noteOffEvent.DeltaTime * divider);
                    _globalRealTime += time;
                    _globalRealTime = (long)playback.GetCurrentTime<MetricTimeSpan>().TotalMilliseconds;
                    Console.WriteLine(@"Simulated input time: " + _globalRealTime);
#if DUMP
                    Bindings.GetInstance().Report.WriteLine("Simulated input time: " + _globalRealTime);
#endif
#if TEST
                    SimulationTime = _globalRealTime;
#endif
                    if (trackData.UsesChannels && trackData.Channels.TryGetValue(noteOffEvent.Channel,out PieceBuffer.ChannelData channelData2))
                    {
                        offset = channelData2.Data;
                    }
                    Bindings.GetInstance().InputBuffer.BufferSimulatedInput(false,(short)(noteOffEvent.NoteNumber + 128 * offset));
                    break;
            }
        }
        
        private void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            bool result = _inputsOffsets.TryGetValue(midiDevice.Name, out InputDeviceData data);
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

        public bool ConfigLoaded = false;
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
            
            _inputDevices.Clear();
            _outputDevices.Clear();
            _inputsOffsets.Clear();
            _outputsData.Clear();

            Bindings.GetInstance().LoadedConfigName = root.Element("configName")?.Value;
            
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
                        _simulationSoundOutput = OutputDevice.GetByName(soundOutputElement.Value);
                        break;
                    }
                    Bindings.GetInstance().FromFile = false;
                    if (inputNames.Contains(inputName))
                    {
                        InputDevice inputDev = InputDevice.GetByName(inputName);
                        inputDev.StartEventsListening();
                        inputDev.EventReceived += OnEventReceived;
                        _inputDevices.Add(inputDev);
                        if (inputMultiChannel)
                        {
                            XElement channelsElement = inputDevice.Element("channels");
                            if(channelsElement == null) return;
                            var channels = channelsElement.Elements("channel");
                            InputDeviceData newInputDevice = new InputDeviceData(0, true);
                            _inputsOffsets.Add(inputName,newInputDevice);
                            foreach (XElement channel in channels)
                            {
                                XElement idElement = channel.Element("id");
                                if(idElement == null) return;
                                int channelId = int.Parse(idElement.Value); 
                                XElement offsetElement = channel.Element("offset");
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
                            _inputsOffsets.Add(inputName,new InputDeviceData(offset,false));
                        }
                    }
                    else
                    {
                        MessageBox.Show("MIDI input device '" + inputName + "' not found! Check if the device is properly connected and if it's correctly defined in config file.", "Error!",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
            }
            ConfigLoaded = true;
            XElement outputs = root.Element("outputs");
            if(outputs is null || !outputs.HasElements) return;
            var outputDevices = outputs.Elements("output");
            var xElements = outputDevices.ToList();
            if(xElements.Count == 0) return;
            for (int i = 0; i < xElements.Count; i++)
            {
                _outputDevices.Add(null);
            }
            foreach (XElement outputDevice in xElements)
            {
                string outputName = outputDevice.Element("name")?.Value;
                if (outputName != null && outputNames.Contains(outputName))
                {
                    OutputDevice outputDev = OutputDevice.GetByName(outputName);
                    
                    XElement deviceIdElement = outputDevice.Element("deviceID");
                    if(deviceIdElement is null) return;
                    int id = int.Parse(deviceIdElement.Value);
                    _outputDevices[id] = outputDev;
                    if (outputMultiChannel)
                    {
                        XElement channelsElement = outputDevice.Element("channels");
                        if(channelsElement == null) return;
                        var channels = channelsElement.Elements("channel");
                        foreach (XElement channel in channels)
                        {
                            XElement globalIdElement = channel.Element("globalID");
                            if(globalIdElement is null) return;
                            int globalId = int.Parse(globalIdElement.Value);
                            XElement channelIdElement = channel.Element("channelID");
                            if(channelIdElement is null) return;
                            int channelId = int.Parse(channelIdElement.Value);
                            _outputsData.Add(globalId,new ChannelIdLink(true,id,channelId));
                        }
                    }
                    else
                    {
                        XElement globalIdElement = outputDevice.Element("globalID");
                        if(globalIdElement is null) return;
                        int globalId = int.Parse(globalIdElement.Value);
                        _outputsData.Add(globalId,new ChannelIdLink(false,id,-1));
                    }
                }
                else
                {
                    MessageBox.Show("MIDI output device '" + outputName + "' not found! Check if the device is properly connected and if it's correctly defined in config file.", "Error!",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
        }
    }
}