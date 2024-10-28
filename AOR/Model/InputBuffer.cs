using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace AOR.Model
{
    public class InputBuffer
    {
        public const int UserBufferSize = 40;
        public const long TickResolution = 1000;
        
        public List<MidiEventData> UserBuffer = new List<MidiEventData>(UserBufferSize);

        private Stopwatch _stopwatch = new Stopwatch();
        private long _previousGlobalTime = 0;
        
        private void BufferInput(MidiEventData value)
        {
             while (UserBuffer.Count >= UserBufferSize)
             {
                 UserBuffer.RemoveAt(0);
             }
             UserBuffer.Add(value);
        }
        
        public void BufferUserInput(bool on ,byte tone)
        {
            long ticks = _stopwatch.ElapsedTicks;
            _stopwatch.Restart();

            ticks /= Stopwatch.Frequency / TickResolution;
            
            //TODO: Calculate delta time
            
            MidiEventData eventData = new MidiEventData(on,tone,ticks,0);
            
            Console.WriteLine(Stopwatch.IsHighResolution + " | " + Stopwatch.Frequency);
            
            if (on)
            {
                Console.WriteLine("Received Note On event. Tone: " + tone + " | DeltaTime: " + ticks);
                
            }
            else
            {
                Console.WriteLine("Received Note Off event. Tone: " + tone + " | DeltaTime: " + ticks);
            }
            BufferInput(eventData);
        }

        public void BufferSimulatedInput(bool on, byte tone, long deltaTime)
        {
            long ticks = _stopwatch.ElapsedTicks;
            _stopwatch.Restart();
            
            ticks /= Stopwatch.Frequency / TickResolution;

            long globalTime = _previousGlobalTime + ticks;
            _previousGlobalTime = globalTime;
            
            MidiEventData eventData = new MidiEventData(on,tone,ticks,globalTime);
            if (on)
            {
                Console.WriteLine("Received Note On event. Tone: " + tone + " | Delta Time: " + ticks);
                
            }
            else
            {
                Console.WriteLine("Received Note Off event. Tone: " + tone + " | Delta Time: " + ticks);
            }
            BufferInput(eventData);
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