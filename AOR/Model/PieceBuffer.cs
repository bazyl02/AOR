using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;

namespace AOR.Model
{
    public class PieceBuffer
    {
        public List<MidiEventData> MelodyBuffer = new List<MidiEventData>();
        public List<MidiEventData> RegistrantsChangesBuffer = new List<MidiEventData>();
        public List<MidiEventData> PageChangesBuffer = new List<MidiEventData>();

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
                                    MelodyBuffer.Add(new MidiEventData(true,onEvent.NoteNumber,localTime,globalTrackTime));
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
                                    MelodyBuffer.Add(new MidiEventData(false,offEvent.NoteNumber,localTime,globalTrackTime));
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
                                    MelodyBuffer.Add(new MidiEventData(false,offEvent.NoteNumber,localTime,globalTrackTime));
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
        }
    }
}