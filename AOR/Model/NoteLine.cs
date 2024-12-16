namespace AOR.Model
{
    public class NoteLine
    {
        public readonly short Tone;
        public readonly uint StartTime;
        public uint EndTime;

        public readonly float StartTimeFloat;
        public float EndTimeFloat;

        public NoteLine(short tone, uint start, uint end)
        {
            Tone = tone;
            StartTime = start;
            EndTime = end;
            StartTimeFloat = start;
            EndTimeFloat = end;
        }

        public override string ToString()
        {
            return @"Tone: " + Tone + @" | Start time: " + StartTime + @" | End time: " + EndTime;
        }
    }
}