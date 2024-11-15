using Melanchall.DryWetMidi.Core;

namespace AOR.Model
{
    public class MidiEventData
    {
        public MidiEvent Event;
        public uint GlobalTime;

        public MidiEventData(MidiEvent midiEvent, uint timeStamp)
        {
            Event = midiEvent;
            GlobalTime = timeStamp;
        }
    }
    
    
}