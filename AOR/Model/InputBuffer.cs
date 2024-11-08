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
        
        private void BufferInput(byte tone, uint timestamp, bool noteOn)
        {
             bool result = _notesInProgress.TryGetValue(tone, out NoteLine line);
             if (result)
             {
                 line.EndTime = timestamp;
                 _notesInProgress.Remove(tone);
                 EndTimestamp = timestamp;
             }
             else
             {
                 if (noteOn)
                 {
                     NoteLine newNoteLine = new NoteLine(tone, timestamp, 0);
                     if (UserBuffer.Count >= UserBufferSize) UserBuffer.RemoveAt(0);
                     UserBuffer.Add(newNoteLine);
                     StartTimestamp = UserBuffer[0].StartTime;
                     _notesInProgress.Add(tone, newNoteLine);
                 }
             }
             Console.WriteLine(StartTimestamp + " | " + EndTimestamp + " | " + UserBuffer.Count);
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
            BufferInput(tone, (uint)ticks, on);
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
            BufferInput(tone,(uint)ticks,on);
        }


        //Clears any previously buffered data
        public void Clear()
        {
            
            StartTimestamp = 0;
            EndTimestamp = 0;
            UserBuffer.Clear();
            _notesInProgress.Clear();
            _stopwatch.Reset();
            _stopwatch.Start();
            Console.WriteLine(_stopwatch.IsRunning);
            _previousGlobalTime = 0;
        }
    }
}