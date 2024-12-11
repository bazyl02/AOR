using Melanchall.DryWetMidi.Core;

namespace AOR.Model
{
    public class MidiEventData
    {
        public int GlobalId;
        public MidiEvent Event;
        public uint GlobalTime;

        public MidiEventData(MidiEvent midiEvent, uint timeStamp,int globalId)
        {
            Event = midiEvent;
            GlobalTime = timeStamp;
            GlobalId = globalId;
        }
    }
    
    
}