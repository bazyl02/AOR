using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace AOR.Model
{
    public class PieceBuffer
    {
        public List<NoteLine> MelodyBuffer = new List<NoteLine>();
        private Dictionary<byte, NoteLine> _notesInProgress = new Dictionary<byte, NoteLine>();
        
        public List<MidiEventData> RegistrantsChangesBuffer = new List<MidiEventData>();
        public List<MidiEventData> PageChangesBuffer = new List<MidiEventData>();

        
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
                                    long localTime = (long)(onEvent.DeltaTime * divider);
                                    
                                    globalTrackTime += localTime;
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
                                    long localTime = (long)(offEvent.DeltaTime * divider);
                                    
                                    globalTrackTime += localTime;
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
                                    long localTime = (long)(offEvent.DeltaTime * divider);
                                    
                                    globalPageTime += localTime;
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
    }
}