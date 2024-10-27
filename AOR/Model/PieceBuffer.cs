using System;
using System.Collections.Generic;
using System.IO;
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
            
            //Iterate through all chunks
            foreach (var chunk in chunks)
            {
                //If it's a track chunk
                if (chunk.ChunkId == "MTrk")
                {
                    //Cast it to track chunk
                    TrackChunk trackChunk = (TrackChunk)chunk;
                    //Get channel(s) used by chunk
                    var trackChannel = Helpers.GetChannels(trackChunk.GetChannels());
                    //If it's more then one abort
                    if (trackChannel.Count > 1) throw new InvalidDataException("Track can't use multiple channels");
                    //If it's zero it's metadata chunk
                    if (trackChannel.Count == 0) continue;
                    //Read track channel number
                    byte channel = trackChannel[0];
                    //Get track's events
                    var events = trackChunk.Events;

                    //If it's track with the smallest channel number it's melody track
                    if (channel == fileChannels[0])
                    {
                        //Count time since the beginning of the track
                        long globalTime = 0;
                        long globalMidiTime = 0;
                        //Iterate through all track's events
                        foreach (var ev in events)
                        {
                            //If it's NoteOn event
                            if (ev.EventType == MidiEventType.NoteOn)
                            {
                                //Cast top NoteOn
                                NoteOnEvent onEvent = (NoteOnEvent)ev;
                                //Calculate global midi time
                                globalMidiTime += onEvent.DeltaTime;
                                //Get tempo value in this event's time
                                long tempo = tempoMap.GetTempoAtTime(new MidiTimeSpan(globalMidiTime)).MicrosecondsPerQuarterNote;
                                //Calculate local time
                                double divider = (tempo / (division * 1.0d)) / InputBuffer.TickResolution * 1.0d;
                                long localTime = (long)(onEvent.DeltaTime * divider);
                                //Calculate global real time
                                globalTime += localTime;
                                //Add tto buffer
                                MelodyBuffer.Add(new MidiEventData(true,onEvent.NoteNumber,localTime,globalTime));
                            } 
                            else if (ev.EventType == MidiEventType.NoteOff)
                            {
                                //Cast top NoteOff
                                NoteOffEvent offEvent = (NoteOffEvent)ev;
                                //Calculate global midi time
                                globalMidiTime += offEvent.DeltaTime;
                                //Get tempo value in this event's time
                                long tempo = tempoMap.GetTempoAtTime(new MidiTimeSpan(globalMidiTime)).MicrosecondsPerQuarterNote;
                                //Calculate local time
                                double divider = (tempo / (division * 1.0d)) / InputBuffer.TickResolution * 1.0d;
                                long localTime = (long)(offEvent.DeltaTime * divider);
                                //Calculate global real time
                                globalTime += localTime;
                                //Add tto buffer
                                MelodyBuffer.Add(new MidiEventData(false,offEvent.NoteNumber,localTime,globalTime));
                            }
                        }
                    } 
                    //else if (channel == fileChannels[1])
                    {
                        
                    }
                    //else if (channel == fileChannels[2])
                    {
                        
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