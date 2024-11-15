using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using System.Threading.Tasks;
using Windows.Data.Pdf;
using Windows.Storage.Streams;
using AOR.ModelView;

namespace AOR.Model
{
    public class PieceBuffer
    {
        public List<NoteLine> MelodyBuffer = new List<NoteLine>();
        private Dictionary<byte, NoteLine> _notesInProgress = new Dictionary<byte, NoteLine>();
        
        public List<MidiEventData> RegistrantsChangesBuffer = new List<MidiEventData>();
        public List<PageData> PageChangesBuffer = new List<PageData>();
        
        private List<BitmapImage> _sheetPages = null;

        private uint _currentTimeValue = 0;

        public uint CurrentTimeValue
        {
            get => _currentTimeValue;
            set
            {
                _currentTimeValue = value;
                TimeValueChanged();
            }
        }

        private void TimeValueChanged()
        {
            
        }
        
        private void AddToMelodyBuffer(byte tone, uint timestamp)
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
        
        public PieceBuffer(MidiFile file)
        {
            
            //Get chunks
            var chunks = file.Chunks;
            //Get all used channels in the list and sort it ascending
            var fileChannels = Helpers.GetChannels(file.GetChannels());
            fileChannels.Sort();
            //Get file time division setting
            short division = 0;
            if (file.TimeDivision is TicksPerQuarterNoteTimeDivision)
            {
                TicksPerQuarterNoteTimeDivision timeDivision = (TicksPerQuarterNoteTimeDivision)file.TimeDivision;
                division = timeDivision.TicksPerQuarterNote;
            }

            TempoMap tempoMap = file.ManageTempoMap().TempoMap;
            
            //Iterate through all chunks
            foreach (var chunk in chunks)
            {
                //If it's a track chunk
                if (chunk.ChunkId == "MTrk")
                {
                    TrackChunk trackChunk = (TrackChunk)chunk;
                    long globalMidiTrackTime = 0;
                    uint globalTrackTime = 0;
                    var events = trackChunk.Events;
                    foreach (var evnt in events)
                    {
                        if (evnt.EventType == MidiEventType.NoteOn || evnt.EventType == MidiEventType.NoteOff)
                        {
                            globalMidiTrackTime += evnt.DeltaTime;
                            long tempo = tempoMap.GetTempoAtTime(new MidiTimeSpan(globalMidiTrackTime)).MicrosecondsPerQuarterNote;
                            double divider = (tempo / (division * 1.0d)) / InputBuffer.TickResolution * 1.0d;
                            uint localTrackTime = (uint)Math.Round(evnt.DeltaTime * divider);
                            globalTrackTime += localTrackTime;
                            NoteEvent noteEvent = (NoteEvent)evnt;
                            //Melody channel
                            if (noteEvent.Channel == fileChannels[0])
                            {
                                AddToMelodyBuffer(noteEvent.NoteNumber,globalTrackTime);
                            }
                            //Register channel
                            else if (noteEvent.Channel == fileChannels[1])
                            {
                                RegistrantsChangesBuffer.Add(new MidiEventData(evnt,globalTrackTime));
                            }
                            //Page channel
                            else if (noteEvent.Channel == fileChannels[2])
                            {
                                    
                            }
                        } 
                        else if (evnt.EventType == MidiEventType.ProgramChange || evnt.EventType == MidiEventType.NormalSysEx)
                        {
                            globalMidiTrackTime += evnt.DeltaTime;
                            long tempo = tempoMap.GetTempoAtTime(new MidiTimeSpan(globalMidiTrackTime)).MicrosecondsPerQuarterNote;
                            double divider = (tempo / (division * 1.0d)) / InputBuffer.TickResolution * 1.0d;
                            uint localTrackTime = (uint)Math.Round(evnt.DeltaTime * divider);
                            globalTrackTime += localTrackTime;
                            RegistrantsChangesBuffer.Add(new MidiEventData(evnt,globalTrackTime));
                        }
                    }
                }
            }
            _notesInProgress.Clear();
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
            if(_sheetPages.Count > 0) Bindings.GetInstance().CurrentSheet = _sheetPages[0];
        }
    }
}