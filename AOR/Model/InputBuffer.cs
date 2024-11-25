﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using AOR.ModelView;

namespace AOR.Model
{
    public class InputBuffer
    {
        public const int UserBufferSize = 20;
        public const long TickResolution = 1000;
        public const int MinimumBufferSize = 10;
        
        public List<NoteLine> UserBuffer = new List<NoteLine>(UserBufferSize);
        private Dictionary<byte, NoteLine> _notesInProgress = new Dictionary<byte, NoteLine>();

        public uint StartTimestamp = 0;
        public uint EndTimestamp = 0;
        
        private Stopwatch _stopwatch = new Stopwatch();
        private long _previousGlobalTime = 0;

        private void DumpUserBufferToReport()
        {
            StreamWriter report = Bindings.GetInstance().Report;
            report.WriteLine("-------------------------------------");
            report.WriteLine("USER BUFFER DATA START");
            report.WriteLine("User buffer size: " + UserBuffer.Count);
            report.WriteLine("Buffer start time: " + StartTimestamp);
            report.WriteLine("Buffer end time: " + EndTimestamp);
            for (int i = 0; i < UserBuffer.Count; i++)
            {
                report.WriteLine(UserBuffer[i] + @" | Index: " + i);
            }
            report.WriteLine("USER BUFFER DATA END");
            report.WriteLine("-------------------------------------");
        }
        
        private void BufferInput(byte tone, uint timestamp, bool noteOn)
        {
             bool result = _notesInProgress.TryGetValue(tone, out NoteLine line);
             if (result)
             {
                 line.EndTime = timestamp;
                 _notesInProgress.Remove(tone);
                 EndTimestamp = timestamp;
                 DumpUserBufferToReport();
                 if(MinimumBufferSize <= UserBuffer.Count)
                 {
                     uint runResult = Bindings.GetInstance().Algorithm.Run();
                     Bindings.GetInstance().PieceBuffer.CurrentTimeValue = runResult;
                     Console.WriteLine(@"Predicted time: " + runResult);
                     Bindings.GetInstance().Report.WriteLine("Predicted time: " + runResult);
                 }
                 Console.WriteLine(@"----------------------------------------------");
                 Bindings.GetInstance().Report.WriteLine("----------------------------------------------");
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
        }
        
        public void BufferUserInput(bool on ,byte tone)
        {
            long ticks = _stopwatch.ElapsedTicks;
            ticks /= Stopwatch.Frequency / TickResolution;
            if (on)
            {
                Console.WriteLine(@"Received Note On event. Tone: " + tone + @" | DeltaTime: " + ticks);
                
            }
            else
            {
                Console.WriteLine(@"Received Note Off event. Tone: " + tone + @" | DeltaTime: " + ticks);
            }
            BufferInput(tone, (uint)ticks, on);
        }
        
        public void BufferSimulatedInput(bool on, byte tone, long deltaTime)
        {
            long ticks = _stopwatch.ElapsedTicks;
            ticks /= Stopwatch.Frequency / TickResolution;
            
            if (on)
            {
                //Console.WriteLine(@"Received Note On event. Tone: " + tone + @" | Delta Time: " + ticks);
                
            }
            else
            {
                //Console.WriteLine(@"Received Note Off event. Tone: " + tone + @" | Delta Time: " + ticks);
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
            _previousGlobalTime = 0;
        }
    }
}