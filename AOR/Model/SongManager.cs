using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Data.Pdf;
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

        public async Task LoadSong(string path)
        {
            using (FileStream zipFile = new FileStream(path,FileMode.Open))
            {
                ZipArchive archive = new ZipArchive(zipFile, ZipArchiveMode.Read);
                MidiFile file = null;
                PdfDocument document = null;
                XDocument config = null;
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.Name.Contains(".pdf"))
                    {
                        MemoryStream stream = new MemoryStream();
                        await entry.Open().CopyToAsync(stream);
                        document = await PdfDocument.LoadFromStreamAsync(stream.AsRandomAccessStream());
                    } 
                    else if (entry.Name.Contains(".mid"))
                    {
                        file = MidiFile.Read(entry.Open());
                    }
                    else if (entry.Name.Contains(".xml"))
                    {
                        config = XDocument.Load(entry.Open());
                    }
                }
                if (file is null || document is null) return;
                PieceData newPiece = new PieceData(file,document,config,path);
                if (!Pieces.Contains(newPiece))
                {
                    Pieces.Add(newPiece);
                }
            }
        }
        
        public class PieceData
        {
            public MidiFile MidiFile;
            public PdfDocument PdfDocument;
            public XDocument Config;
            public string Path;
            public string SongName;

            public PieceData(MidiFile file, PdfDocument document,XDocument config, string path)
            {
                MidiFile = file;
                PdfDocument = document;
                Config = config;
                Path = path;
                SongName = System.IO.Path.GetFileNameWithoutExtension(Path);
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
                hash = (hash * 17) + SongName.GetHashCode();
                return hash;
            }

            public override string ToString()
            {
                return SongName;
            }
        }
    }
}