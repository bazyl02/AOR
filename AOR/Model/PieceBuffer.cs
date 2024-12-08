using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public readonly List<NoteLine> MelodyBuffer = new List<NoteLine>();
        private readonly Dictionary<short, NoteLine> _notesInProgress = new Dictionary<short, NoteLine>();
        
        private readonly List<MidiEventData> _registrantsChangesBuffer = new List<MidiEventData>();
        private List<PageData> _pageChangesBuffer = new List<PageData>();
        
        private List<BitmapImage> _sheetPages = null;
        
        
        private uint _previousTimeValue = 0;
        private uint _currentTimeValue = 0;
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
                if (page.StartTimeStamp <= _currentTimeValue && page.EndTimeStamp >= _currentTimeValue)
                {
                    float value = 1.0f - (_currentTimeValue - page.StartTimeStamp *1.0f) / (page.EndTimeStamp - page.StartTimeStamp);
                    Bindings.GetInstance().SheetWindow.MoveSheets(value);
                } 
                else if (page.EndTimeStamp <= _currentTimeValue && page.EndTimeStamp >= _previousTimeValue)
                {
                    Bindings.GetInstance().CurrentSheet = _sheetPages[page.PageNumber];
                    Bindings.GetInstance().NewSheet = _pageChangesBuffer.Count > index + 1 ? _sheetPages[_pageChangesBuffer[index + 1].PageNumber] : null;
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
        
        public PieceBuffer(MidiFile file)
        {
            if (file.OriginalFormat != MidiFileFormat.MultiTrack)
                throw new InvalidDataException("Selected MIDI file has to be of type 1 (multi-track)");
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
                        switch (index)
                        {
                            //Melody
                            case 0:
                            {
                                if (midiEvent.EventType == MidiEventType.NoteOn || midiEvent.EventType == MidiEventType.NoteOff)
                                {
                                    NoteEvent noteEvent = (NoteEvent)midiEvent;
                                    AddToMelodyBuffer(noteEvent.NoteNumber,globalTrackTime);
                                }
                                break;
                            }
                            //Register changes
                            case 1:
                            {
                                if (midiEvent.EventType == MidiEventType.NoteOn ||
                                    midiEvent.EventType == MidiEventType.NoteOff ||
                                    midiEvent.EventType == MidiEventType.ProgramChange ||
                                    midiEvent.EventType == MidiEventType.NormalSysEx)
                                {
                                    _registrantsChangesBuffer.Add(new MidiEventData(midiEvent,globalTrackTime));
                                }
                                break;
                            }
                            //Page changes
                            case 2:
                            {
                                if (midiEvent.EventType == MidiEventType.NoteOn)
                                {
                                    NoteOnEvent onEvent = (NoteOnEvent)midiEvent;
                                    _pageChangesBuffer.Add(new PageData(onEvent.NoteNumber,globalTrackTime,0));
                                } 
                                else if (midiEvent.EventType == MidiEventType.NoteOff)
                                {
                                    NoteOffEvent offEvent = (NoteOffEvent)midiEvent;
                                    _pageChangesBuffer.Last().EndTimeStamp = globalTrackTime;
                                }
                                break;
                            }
                        }
                    }
                    index++;
                }
                if(index >= 3) break;
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
            if(_sheetPages.Count > 1) Bindings.GetInstance().NewSheet = _sheetPages[1];
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