namespace AOR.Model
{
    public class NoteLine
    {
        public short Tone;
        public uint StartTime;
        public uint EndTime;

        public NoteLine(short tone, uint start, uint end)
        {
            Tone = tone;
            StartTime = start;
            EndTime = end;
        }

        public override string ToString()
        {
            return @"Tone: " + Tone + @" | Start time: " + StartTime + @" | End time: " + EndTime;
        }
    }
}