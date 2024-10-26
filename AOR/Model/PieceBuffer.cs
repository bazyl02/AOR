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
                Console.WriteLine(chunk.ChunkId);
            }
        }
    }
}