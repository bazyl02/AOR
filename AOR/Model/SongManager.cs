using System.Collections.Generic;
using Melanchall.DryWetMidi.Core;

namespace AOR.Model
{
    public class SongManager
    {
        public SongManager()
        {
            Songs = new List<MidiFile>();
        }
        
        public List<MidiFile> Songs;

        public void LoadSong(string path)
        {
            MidiFile song = MidiFile.Read(path);
            if(!Songs.Contains(song)) Songs.Add(song);
        }
        
    }
}