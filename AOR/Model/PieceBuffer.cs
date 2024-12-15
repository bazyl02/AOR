using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Data.Pdf;
using Windows.Storage.Streams;
using AOR.ModelView;

namespace AOR.Model
{
    public class PieceBuffer
    {
        public readonly List<NoteLine> MelodyBuffer = new List<NoteLine>();
        private readonly Dictionary<short, NoteLine> _notesInProgress = new Dictionary<short, NoteLine>();
        
        private readonly List<MidiEventData> _registrantsChangesBuffer = new List<MidiEventData>();
        private readonly List<PageData> _pageChangesBuffer = new List<PageData>();
        
        private List<BitmapImage> _sheetPages;
        
        
        private uint _previousTimeValue;
        private uint _currentTimeValue;
        public uint CurrentTimeValue
        {
            get => _currentTimeValue;
            set
            {
                if (_previousTimeValue == 0 && _currentTimeValue == 0)
                {
                    _previousTimeValue = value;
                    _currentTimeValue = value;
                }
                else
                {
                    _previousTimeValue = _currentTimeValue;
                    _currentTimeValue = value;
                }
                TimeValueChanged();
            }
        }

        private void TimeValueChanged()
        {
            //Check for registrant change events
            foreach (MidiEventData eventData in _registrantsChangesBuffer)
            {
                if (eventData.GlobalTime >= _previousTimeValue && eventData.GlobalTime <= _currentTimeValue)
                {
                    Bindings.GetInstance().DeviceController.SendToOutput(eventData);
                }
            }
            //Check for page change events
            int index = 0;
            foreach (PageData page in _pageChangesBuffer)
            {
                if (page.StartTimeStamp <= _currentTimeValue && page.EndTimeStamp >= _currentTimeValue && !page.Fired && Bindings.GetInstance().NewSheet != null)
                {
                    page.Fired = true;
                    //float value = 1.0f - (_currentTimeValue - page.StartTimeStamp *1.0f) / (page.EndTimeStamp - page.StartTimeStamp);
                    //Bindings.GetInstance().SheetWindow.MoveSheets(value);
                    Bindings.GetInstance().SheetWindow.AnimateSheets(page.EndTimeStamp - page.StartTimeStamp);
                } 
                else if (page.EndTimeStamp <= _currentTimeValue && page.EndTimeStamp >= _previousTimeValue && page.Fired)
                {
                    page.Fired = false;
                    Bindings.GetInstance().CurrentSheet2 = _pageChangesBuffer.Count > index + 1 ? _sheetPages[_pageChangesBuffer[index + 1].PageNumber] : null;
                    Bindings.GetInstance().CurrentSheet = _pageChangesBuffer.Count > index ? _sheetPages[_pageChangesBuffer[index].PageNumber] : null;
                    Bindings.GetInstance().NewSheet = _pageChangesBuffer.Count > index + 2 ? _sheetPages[_pageChangesBuffer[index + 2].PageNumber] : null;
                    Bindings.GetInstance().SheetWindow.ResetAll();
                }
                index++;
            }
        }
        
        private void AddToMelodyBuffer(short tone, uint timestamp)
        {
            bool result = _notesInProgress.TryGetValue(tone, out NoteLine line);
            if (result)
            {
                line.EndTime = timestamp;
                _notesInProgress.Remove(tone);
            }
            else
            {
                NoteLine newNoteLine = new NoteLine(tone, timestamp, 0);
                MelodyBuffer.Add(newNoteLine);
                _notesInProgress.Add(tone, newNoteLine);
            }
        }
        
        public PieceBuffer(MidiFile file, XDocument config)
        {
            if (file.OriginalFormat != MidiFileFormat.MultiTrack)
                throw new InvalidDataException("Selected MIDI file has to be of type 1 (multi-track)");
            
            var configData = ParseConfig(config);
            
            //Get chunks
            var chunks = file.Chunks;
            //Get file time division setting
            short division = 0;
            if (file.TimeDivision is TicksPerQuarterNoteTimeDivision)
            {
                TicksPerQuarterNoteTimeDivision timeDivision = (TicksPerQuarterNoteTimeDivision)file.TimeDivision;
                division = timeDivision.TicksPerQuarterNote;
            }
            TempoMap tempoMap = file.ManageTempoMap().TempoMap;
            int index = 0;
            //Iterate through all chunks
            foreach (MidiChunk chunk in chunks)
            {
                if (chunk.ChunkId == "MTrk")
                {
                    Console.WriteLine(@"Chunk no. " + index);
                    if (!configData.TryGetValue(index, out TrackData trackData)) continue;
                    TrackChunk trackChunk = (TrackChunk)chunk;
                    long globalMidiTrackTime = 0;
                    uint globalTrackTime = 0;
                    var events = trackChunk.Events;
                    foreach (MidiEvent midiEvent in events)
                    {
                        globalMidiTrackTime += midiEvent.DeltaTime;
                        long tempo = tempoMap.GetTempoAtTime(new MidiTimeSpan(globalMidiTrackTime)).MicrosecondsPerQuarterNote;
                        double divider = (tempo / (division * 1.0d)) / InputBuffer.TickResolution * 1.0d;
                        uint localTrackTime = (uint)Math.Round(midiEvent.DeltaTime * divider);
                        globalTrackTime += localTrackTime;
                        switch (midiEvent.EventType)
                        {
                            case MidiEventType.NoteOn:
                            {
                                NoteOnEvent castEvent = (NoteOnEvent)midiEvent;
                                if (trackData.UsesChannels && trackData.Channels.TryGetValue(castEvent.Channel,out ChannelData channelData))
                                {
                                    switch (channelData.Use)
                                    {
                                        case Usage.Melody:
                                        {
                                            AddToMelodyBuffer((short)(castEvent.NoteNumber + 128 * channelData.Data),globalTrackTime);
                                            break;
                                        }
                                        case Usage.Registers:
                                        {
                                            if (channelData.RegisterFormat == RegisterFormat.Note)
                                            {
                                                _registrantsChangesBuffer.Add(new MidiEventData(midiEvent,globalTrackTime,channelData.Data));
                                            }
                                            break;
                                        }
                                        case Usage.Pages:
                                        {
                                            _pageChangesBuffer.Add(new PageData(castEvent.NoteNumber,globalTrackTime,0));
                                            break;
                                        }
                                    }
                                }
                                else if(!trackData.UsesChannels)
                                {
                                    switch (trackData.Use)
                                    {
                                        case Usage.Melody:
                                        {
                                            AddToMelodyBuffer((short)(castEvent.NoteNumber + 128 * trackData.Data),globalTrackTime);
                                            break;
                                        }
                                        case Usage.Registers:
                                        {
                                            if (trackData.RegisterFormat == RegisterFormat.Note)
                                            {
                                                _registrantsChangesBuffer.Add(new MidiEventData(midiEvent,globalTrackTime,trackData.Data));
                                            }
                                            break;
                                        }
                                        case Usage.Pages:
                                        {
                                            _pageChangesBuffer.Add(new PageData(castEvent.NoteNumber,globalTrackTime,0));
                                            break;
                                        }
                                    }
                                }
                                break;
                            }
                            case MidiEventType.NoteOff:
                            {
                                NoteOffEvent castEvent = (NoteOffEvent)midiEvent;
                                if (trackData.UsesChannels && trackData.Channels.TryGetValue(castEvent.Channel,out ChannelData channelData))
                                {
                                    switch (channelData.Use)
                                    {
                                        case Usage.Melody:
                                        {
                                            AddToMelodyBuffer((short)(castEvent.NoteNumber + 128 * channelData.Data),globalTrackTime);
                                            break;
                                        }
                                        case Usage.Registers:
                                        {
                                            if (channelData.RegisterFormat == RegisterFormat.Note)
                                            {
                                                _registrantsChangesBuffer.Add(new MidiEventData(midiEvent,globalTrackTime,channelData.Data));
                                            }
                                            break;
                                        }
                                        case Usage.Pages:
                                        {
                                            _pageChangesBuffer.Last().EndTimeStamp = globalTrackTime;
                                            break;
                                        }
                                    }
                                }
                                else if(!trackData.UsesChannels)
                                {
                                    switch (trackData.Use)
                                    {
                                        case Usage.Melody:
                                        {
                                            AddToMelodyBuffer((short)(castEvent.NoteNumber + 128 * trackData.Data),globalTrackTime);
                                            break;
                                        }
                                        case Usage.Registers:
                                        {
                                            if (trackData.RegisterFormat == RegisterFormat.Note)
                                            {
                                                _registrantsChangesBuffer.Add(new MidiEventData(midiEvent,globalTrackTime,trackData.Data));
                                            }
                                            break;
                                        }
                                        case Usage.Pages:
                                        {
                                            _pageChangesBuffer.Last().EndTimeStamp = globalTrackTime;
                                            break;
                                        }
                                    }
                                }
                                break;
                            }
                            case MidiEventType.ProgramChange:
                            {
                                ProgramChangeEvent castEvent = (ProgramChangeEvent)midiEvent;
                                if (trackData.UsesChannels && trackData.Channels.TryGetValue(castEvent.Channel,out ChannelData channelData))
                                {
                                    if (channelData.Use == Usage.Registers)
                                    {
                                        if (channelData.RegisterFormat == RegisterFormat.ProgramChange)
                                        {
                                            _registrantsChangesBuffer.Add(new MidiEventData(midiEvent,globalTrackTime,channelData.Data));
                                        }
                                    }
                                }
                                else if(!trackData.UsesChannels)
                                {
                                    if (trackData.Use == Usage.Registers)
                                    {
                                        if (trackData.RegisterFormat == RegisterFormat.ProgramChange)
                                        {
                                            _registrantsChangesBuffer.Add(new MidiEventData(midiEvent,globalTrackTime,trackData.Data));
                                        }
                                    }
                                }
                                break;
                            }
                            case MidiEventType.NormalSysEx:
                            {
                                //NormalSysExEvent castEvent = (NormalSysExEvent)midiEvent;
                                if(!trackData.UsesChannels && trackData.Use == Usage.Registers && trackData.RegisterFormat == RegisterFormat.SysEx)
                                { 
                                    _registrantsChangesBuffer.Add(new MidiEventData(midiEvent,globalTrackTime,trackData.Data));
                                }
                                break;
                            }
                        }
                    }
                    index++;
                }
            }

            if (_pageChangesBuffer.Count > 0)
            {
                int rootPageNum = _pageChangesBuffer[0].PageNumber;
                foreach (var page in _pageChangesBuffer)
                {
                    page.PageNumber -= rootPageNum;
                }
            }
            _notesInProgress.Clear();
        }

        public enum Usage
        {
            None,
            Melody,
            Registers,
            Pages
        }

        public enum RegisterFormat
        {
            NotApply,
            Note,
            ProgramChange,
            SysEx
        }
        
        public class TrackData
        {
            public readonly Usage Use;
            public RegisterFormat RegisterFormat = RegisterFormat.NotApply;
            public readonly bool UsesChannels;
            public readonly int Data;
            public readonly Dictionary<int, ChannelData> Channels = new Dictionary<int, ChannelData>();


            public TrackData(Usage use, int data)
            {
                Use = use;
                UsesChannels = false;
                Data = data;
            }
            
            public TrackData()
            {
                Use = Usage.None;
                UsesChannels = true;
            }
            
        }

        public class ChannelData
        {
            public readonly Usage Use;
            public RegisterFormat RegisterFormat = RegisterFormat.NotApply;
            public readonly int Data;

            public ChannelData(Usage use, int data)
            {
                Use = use;
                Data = data;
            }
        }

        private Dictionary<int, TrackData> ParseConfig(XDocument config)
        {
            Dictionary<int, TrackData> output = new Dictionary<int, TrackData>();
            var tracks = config.Root?.Element("tracks")?.Elements("track");
            if (tracks is null) throw new ArgumentException("No 'tracks' tag in melody config file!");
            foreach (XElement track in tracks)
            {
                XElement trackIdElement = track.Element("trackID");
                if (trackIdElement is null)
                    throw new ArgumentException("At least one of the tracks does not have 'trackID' tag");
                int trackId = int.Parse(trackIdElement.Value);
                XElement usesChannelsElement = track.Element("usesMultiChannel");
                if (usesChannelsElement is null) throw new ArgumentException("At least one of the tracks does not have 'usesMultiChannel' tag");
                bool usesChannels = usesChannelsElement.Value == "True";
                TrackData trackData = null;
                if (usesChannels)
                {
                    trackData = new TrackData();
                    var channels = track.Element("channels")?.Elements("channel");
                    if (channels is null) throw new ArgumentException("At least one of the tracks does not have 'channels' tag even thought track is marked as multichannel one");
                    foreach (XElement channel in channels)
                    {
                        ChannelData channelData = null;
                        XElement channelIdElement = channel.Element("channelID");
                        if (channelIdElement is null || !int.TryParse(channelIdElement.Value,out int channelId))
                            throw new ArgumentException("At least one of the channels in track no." + trackId +
                                                        " does not have proper 'channelID' tag");
                        XElement usageElement = channel.Element("use");
                        if (usageElement is null || !Enum.TryParse(usageElement.Value,out Usage channelUsage)) throw new ArgumentException("At least one of the channels in track no." + trackId +" does not have 'use' tag even thought track is marked as multichannel one");
                        if (channelUsage == Usage.Melody)
                        {
                            XElement dataElement = channel.Element("offset");
                            if (dataElement is null || !int.TryParse(dataElement.Value,out int dt)) throw new ArgumentException("At least one of channels in track no." + trackId +" does not have 'offset' tag");
                            channelData = new ChannelData(channelUsage, dt);
                        }
                        else if (channelUsage == Usage.Registers)
                        {
                            XElement dataElement = channel.Element("globalID");
                            if (dataElement is null || !int.TryParse(dataElement.Value,out int dt)) throw new ArgumentException("At least one of channels in track no." + trackId +" does not have 'globalID' tag");
                            channelData = new ChannelData(channelUsage, dt);
                            XElement formatElement = channel.Element("registerFormat");
                            if (formatElement is null || !Enum.TryParse(formatElement.Value, out RegisterFormat registerFormat))
                                throw new ArgumentException("At least one of register channels in track no." + trackId +" does not have 'registerFormat' tag");
                            channelData.RegisterFormat = registerFormat;
                        } 
                        else if (channelUsage == Usage.Pages)
                        {
                            channelData = new ChannelData(channelUsage, -1);
                        }
                        trackData.Channels.Add(channelId,channelData);
                    }
                }
                else
                {
                    XElement usageElement = track.Element("use");
                    if (usageElement is null || !Enum.TryParse(usageElement.Value,out Usage trackUsage)) throw new ArgumentException("Track no." + trackId +" does not have 'use' tag even thought track is not marked as multichannel one");
                    if (trackUsage == Usage.Melody)
                    {
                        XElement dataElement = track.Element("offset");
                        if (dataElement is null || !int.TryParse(dataElement.Value,out int dt)) throw new ArgumentException("At least one of melody tracks does not have 'offset' tag");
                        trackData = new TrackData(trackUsage, dt);
                    } 
                    else if (trackUsage == Usage.Registers)
                    {
                        XElement dataElement = track.Element("globalID");
                        if (dataElement is null || !int.TryParse(dataElement.Value,out int dt)) throw new ArgumentException("At least one of register tracks does not have 'globalID' tag");
                        trackData = new TrackData(trackUsage, dt);
                        XElement formatElement = track.Element("registerFormat");
                        if (formatElement is null || !Enum.TryParse(formatElement.Value, out RegisterFormat registerFormat))
                            throw new ArgumentException("At least one of register tracks does not have 'registerFormat' tag");
                        trackData.RegisterFormat = registerFormat;
                    } 
                    else if (trackUsage == Usage.Pages)
                    {
                        trackData = new TrackData(trackUsage, -1);
                    }
                }
                output.Add(trackId,trackData);
            }

            return output;
        }
        
        public async Task LoadPdfPages(PdfDocument pdfDocument)
        {
            List<BitmapImage> output = new List<BitmapImage>();
            for (uint i = 0; i < pdfDocument.PageCount; i++)
            {
                using (PdfPage page = pdfDocument.GetPage(i))
                {
                    using (InMemoryRandomAccessStream memStream = new InMemoryRandomAccessStream())
                    {
                        await page.RenderToStreamAsync(memStream);
                        var bi = new BitmapImage(); 
                        bi.BeginInit();
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.StreamSource = memStream.AsStream();
                        bi.EndInit();
                        output.Add(bi);
                    }
                }
            }
            _sheetPages = output;
            if(_pageChangesBuffer.Count > 0 && _sheetPages.Count > 0) Bindings.GetInstance().CurrentSheet = _sheetPages[_pageChangesBuffer[0].PageNumber];
            if(_pageChangesBuffer.Count > 1 &&_sheetPages.Count > 1) Bindings.GetInstance().CurrentSheet2 = _sheetPages[_pageChangesBuffer[1].PageNumber];
            if(_pageChangesBuffer.Count > 2 &&_sheetPages.Count > 2) Bindings.GetInstance().NewSheet = _sheetPages[_pageChangesBuffer[2].PageNumber];
        }

#if DUMP
        public void DumpMelodyBufferToReport()
        {
            StreamWriter report = Bindings.GetInstance().Report;
            report.WriteLine("-------------------------------------");
            report.WriteLine("MELODY DATA START");
            report.WriteLine("Melody name: " + Bindings.GetInstance().SelectedPiece.SongName);
            for (int i = 0; i < MelodyBuffer.Count; i++)
            {
                report.WriteLine(MelodyBuffer[i] + @" | Index: " + i);
            }
            report.WriteLine("MELODY DATA END");
            report.WriteLine("-------------------------------------");
        }
#endif
    }
}