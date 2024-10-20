using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Melanchall.DryWetMidi.Core;

namespace AOR.Model
{
    public class SongManager
    {
        public SongManager()
        {
            Pieces = new List<PieceData>();
        }
        
        public List<PieceData> Pieces;

        public void LoadSong(string path)
        {
            MidiFile song = MidiFile.Read(path);
            PieceData newPiece = new PieceData(song, path);
            if (!Pieces.Contains(newPiece))
            {
                Pieces.Add(newPiece);
            }
        }
        
        public class PieceData
        {
            public MidiFile MidiFile;
            public string Path;
            public string Name;

            public PieceData(MidiFile file, string path)
            {
                MidiFile = file;
                Path = path;
                Name = System.IO.Path.GetFileNameWithoutExtension(Path);
            }

            public override bool Equals(object obj)
            {
                if (obj == null) return false;
                if (GetType() != obj.GetType()) return false;
                PieceData data = (PieceData)obj;
                return Path.Equals(data.Path);
            }

            public override int GetHashCode()
            {
                int hash = 97;
                hash = (hash * 17) + MidiFile.GetHashCode();
                hash = (hash * 17) + Name.GetHashCode();
                return hash;
            }
        }
    }
}