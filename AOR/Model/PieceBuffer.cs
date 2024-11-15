﻿using System;
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
        public List<MidiEventData> PageChangesBuffer = new List<MidiEventData>();
        
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

            long globalTrackTime = 0;
            long globalRegisterTime = 0;
            long globalPageTime = 0;

            long globalMidiTrackTime = 0;
            long globalMidiRegisterTime = 0;
            long globalMidiPageTime = 0;
            
            //Iterate through all chunks
            foreach (var chunk in chunks)
            {
                //If it's a track chunk
                if (chunk.ChunkId == "MTrk")
                {
                    TrackChunk trackChunk = (TrackChunk)chunk;
                    var events = trackChunk.Events;
                    foreach (var evnt in events)
                    {
                        switch (evnt.EventType)
                        {
                            case MidiEventType.NoteOn:
                            {
                                NoteOnEvent onEvent = (NoteOnEvent)evnt;
                                //Melody channel
                                if (onEvent.Channel == fileChannels[0])
                                {
                                    globalMidiTrackTime += onEvent.DeltaTime;
                                    long tempo = tempoMap.GetTempoAtTime(new MidiTimeSpan(globalMidiTrackTime)).MicrosecondsPerQuarterNote;
                                    double divider = (tempo / (division * 1.0d)) / InputBuffer.TickResolution * 1.0d;
                                    globalTrackTime = (long)Math.Round(globalMidiTrackTime * divider);
                                    AddToMelodyBuffer(onEvent.NoteNumber,(uint)globalTrackTime);
                                }
                                //Register channel
                                else if (onEvent.Channel == fileChannels[1])
                                {
                                    
                                }
                                //Page channel
                                else if (onEvent.Channel == fileChannels[2])
                                {
                                    
                                }
                                break;
                            }
                            case MidiEventType.NoteOff:
                            {
                                NoteOffEvent offEvent = (NoteOffEvent)evnt;
                                //Melody channel
                                if (offEvent.Channel == fileChannels[0])
                                {
                                    globalMidiTrackTime += offEvent.DeltaTime;
                                    long tempo = tempoMap.GetTempoAtTime(new MidiTimeSpan(globalMidiTrackTime)).MicrosecondsPerQuarterNote;
                                    double divider = (tempo / (division * 1.0d)) / InputBuffer.TickResolution * 1.0d;
                                    globalTrackTime = (long)Math.Round(globalMidiTrackTime * divider);
                                    AddToMelodyBuffer(offEvent.NoteNumber,(uint)globalTrackTime);
                                }
                                //Register channel
                                else if (offEvent.Channel == fileChannels[1])
                                {
                                    
                                }
                                //Page channel
                                else if (offEvent.Channel == fileChannels[2])
                                {
                                    globalMidiPageTime += offEvent.DeltaTime;
                                    long tempo = tempoMap.GetTempoAtTime(new MidiTimeSpan(globalMidiPageTime)).MicrosecondsPerQuarterNote;
                                    double divider = (tempo / (division * 1.0d)) / InputBuffer.TickResolution * 1.0d;
                                    globalPageTime = (long)Math.Round(globalMidiPageTime * divider);
                                    //MelodyBuffer.Add(new MidiEventData(false,offEvent.NoteNumber,localTime,globalTrackTime));
                                }
                                break;
                            }
                            case MidiEventType.ProgramChange:
                            {
                                ProgramChangeEvent prEvent = (ProgramChangeEvent)evnt;
                                break;
                            }
                            
                        }
                    }
                }
                else
                {
                    Console.WriteLine(@"Header chunk detected!");
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