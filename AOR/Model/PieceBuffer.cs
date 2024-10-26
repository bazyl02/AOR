using System;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;

namespace AOR.Model
{
    public class PieceBuffer
    {
        public List<MidiEventData> SongBuffer = new List<MidiEventData>();

        public PieceBuffer(MidiFile file)
        {
            var chunks = file.Chunks;
            foreach (var chunk in chunks)
            {
                if (chunk.ChunkId == "MTrk")
                {
                    TrackChunk trackChunk = (TrackChunk)chunk;
                    var events = trackChunk.Events;
                    var channels = trackChunk.GetChannels();
                    foreach (var channel in channels)
                    {
                        Console.WriteLine(channel);
                    }
                    foreach (var ev in events)
                    {
                        //Console.WriteLine(ev.EventType);
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