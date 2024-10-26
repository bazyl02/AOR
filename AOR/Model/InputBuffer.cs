using System;
using System.Collections.Generic;

namespace AOR.Model
{
    public class InputBuffer
    {
        public const int UserBufferSize = 40;
        
        public List<MidiEventData> UserBuffer = new List<MidiEventData>(UserBufferSize);
        
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
            //TODO: Calculate and pass delta time and global time
            MidiEventData eventData = new MidiEventData(on,tone,0,0);
            
            if (on)
            {
                Console.WriteLine("Received Note On event. Tone: " + tone);
                
            }
            else
            {
                Console.WriteLine("Received Note Off event. Tone: " + tone);
            }
            BufferInput(eventData);
        }

        public void BufferSimulatedInput(bool on, byte tone, long deltaTime)
        {
            //TODO: Calculate and pass global time
            MidiEventData eventData = new MidiEventData(on,tone,deltaTime,0);
            if (on)
            {
                Console.WriteLine("Received Note On event. Tone: " + tone + " | Delta Time: " + deltaTime);
                
            }
            else
            {
                Console.WriteLine("Received Note Off event. Tone: " + tone + " | Delta Time: " + deltaTime);
            }
            BufferInput(eventData);
        }
    }
}