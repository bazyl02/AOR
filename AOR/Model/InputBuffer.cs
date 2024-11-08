using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AOR.Model
{
    public class InputBuffer
    {
        public const int UserBufferSize = 40;
        public const long TickResolution = 1000;
        
        public List<NoteLine> UserBuffer = new List<NoteLine>(UserBufferSize);
        private Dictionary<byte, NoteLine> _notesInProgress = new Dictionary<byte, NoteLine>();

        public uint StartTimestamp = 0;
        public uint EndTimestamp = 0;
        
        private Stopwatch _stopwatch = new Stopwatch();
        private long _previousGlobalTime = 0;
        
        private void BufferInput(byte tone, uint timestamp)
        {
             bool result = _notesInProgress.TryGetValue(tone, out NoteLine line);
             if (result)
             {
                 line.EndTime = timestamp;
                 _notesInProgress.Remove(tone);
             }
             else
             {
                 NoteLine newNoteLine = new NoteLine(tone, timestamp, 0);
                 if (UserBuffer.Count >= UserBufferSize) UserBuffer.RemoveAt(0);
                 UserBuffer.Add(newNoteLine);
                 StartTimestamp = UserBuffer[0].StartTime;
                 EndTimestamp = UserBuffer[UserBuffer.Count - 1].EndTime;
                 _notesInProgress.Add(tone, newNoteLine);
             }
        }
        
        public void BufferUserInput(bool on ,byte tone)
        {
            long ticks = _stopwatch.ElapsedTicks;
            ticks /= Stopwatch.Frequency / TickResolution;
            if (on)
            {
                Console.WriteLine("Received Note On event. Tone: " + tone + " | DeltaTime: " + ticks);
                
            }
            else
            {
                Console.WriteLine("Received Note Off event. Tone: " + tone + " | DeltaTime: " + ticks);
            }
            BufferInput(tone, (uint)ticks);
        }

        public void BufferSimulatedInput(bool on, byte tone, long deltaTime)
        {
            long ticks = _stopwatch.ElapsedTicks;
            ticks /= Stopwatch.Frequency / TickResolution;
            if (on)
            {
                Console.WriteLine("Received Note On event. Tone: " + tone + " | Delta Time: " + ticks);
                
            }
            else
            {
                Console.WriteLine("Received Note Off event. Tone: " + tone + " | Delta Time: " + ticks);
            }
            BufferInput(tone,(uint)ticks);
        }


        //Clears any previously buffered data
        public void Clear()
        {
            UserBuffer.Clear();
            _stopwatch.Reset();
            _previousGlobalTime = 0;
        }
    }
}