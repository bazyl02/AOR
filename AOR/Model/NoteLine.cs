namespace AOR.Model
{
    public class NoteLine
    {
        public byte Tone;
        public uint StartTime;
        public uint EndTime;

        public NoteLine(byte tone, uint start, uint end)
        {
            Tone = tone;
            StartTime = start;
            EndTime = end;
        }
    }
}