using System;
using System.Collections.Generic;

namespace AOR.Model
{
    public class InputBuffer
    {
        public const int UserBufferSize = 40;
        
        public List<int> UserBuffer = new List<int>(UserBufferSize);
        
        private void BufferInput(int value)
        {
             while (UserBuffer.Count >= UserBufferSize)
             {
                 UserBuffer.RemoveAt(0);
             }
             UserBuffer.Add(value);
        }
        
        public void BufferUserInput(bool on ,byte tone)
        {
            int bufferedValue = 0;
            if (on)
            {
                Console.WriteLine("Received Note On event. Tone: " + tone);
                bufferedValue += 1;
            }
            else
            {
                Console.WriteLine("Received Note Off event. Tone: " + tone);
            }

            bufferedValue <<= 7;
            bufferedValue += tone;
            Console.WriteLine(Convert.ToString(bufferedValue,2).PadLeft(16,'0'));
            BufferInput(bufferedValue);
        }

        public void BufferSimulatedInput(bool on, byte tone, long deltaTime)
        {
            int bufferedValue = 0;
            if (on)
            {
                Console.WriteLine("Received Note On event. Tone: " + tone + " | Delta Time: " + deltaTime);
                bufferedValue += 1;
            }
            else
            {
                Console.WriteLine("Received Note Off event. Tone: " + tone + " | Delta Time: " + deltaTime);
            }
            bufferedValue <<= 7;
            bufferedValue += tone;
            bufferedValue <<= 16;
            bufferedValue += (int)deltaTime;
            Console.WriteLine(Convert.ToString(bufferedValue,2).PadLeft(24,'0'));
            BufferInput(bufferedValue);
        }
    }
}