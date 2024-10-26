namespace AOR.Model
{
    public class MidiEventData
    {
        public bool NoteOn;
        public byte Tone;
        public long DeltaTime;

        public long GlobalTime;

        public MidiEventData(bool isOn, byte tone, long dT, long gT)
        {
            NoteOn = isOn;
            Tone = tone;
            DeltaTime = dT;
            GlobalTime = gT;
        }
    }
    
    
}