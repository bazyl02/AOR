#define StaticWindowSize

using System;
using System.Diagnostics;
using AOR.ModelView;

namespace AOR.Model
{
    public class Algorithm
    {
        private const float MinimumPer = 0.9f;
        private const int HighestAmount = 6;
        #if !StaticWindowSize
            private const float WindowSize = 0.5f;
        #else 
            private const int WindowSize = 512;
        #endif
        
        private readonly PieceBuffer _pieceBuffer = Bindings.GetInstance().PieceBuffer;
        private readonly InputBuffer _inputBuffer = Bindings.GetInstance().InputBuffer;

        private int _previousHighestIndex = -1;
        private readonly float[] _highestRatios = new float[HighestAmount];
        private readonly int[] _highestRatioIndices = new int[HighestAmount];

        public uint Run()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            float highestRatio = 0;
            int highestRatioIndex = 0;
            for (int i = 0; i < HighestAmount; i++)
            {
                _highestRatios[i] = -1.0f;
            }
#if !StaticWindowSize
            int windowSpan = (int)(_pieceBuffer.MelodyBuffer.Count * WindowSize);
            int startIndex = Math.Max(_previousHighestIndex - windowSpan,0);
            int endIndex = Math.Min(_previousHighestIndex + windowSpan,_pieceBuffer.MelodyBuffer.Count);
#else
            int startIndex = Math.Max(_previousHighestIndex - WindowSize,0);
            int endIndex = Math.Min(_previousHighestIndex + WindowSize,_pieceBuffer.MelodyBuffer.Count);
#endif
            for (int i = startIndex; i < endIndex; i++)
            {
                uint bufferTime = _pieceBuffer.MelodyBuffer[i].EndTime;
                if(bufferTime < _inputBuffer.EndTimestamp - _inputBuffer.StartTimestamp) continue;
                float sum = 0;
                int amount = 0;
                for (int j = i; j >= 0; j--)
                {
                    NoteLine melody = _pieceBuffer.MelodyBuffer[j];
                    //Translate timestamps from global space to local buffer space
                    uint melodyLocalStart = bufferTime - melody.StartTime;
                    uint melodyLocalEnd = melody.EndTime > bufferTime ? 0 : bufferTime - melody.EndTime;
                    if(melodyLocalStart > _inputBuffer.EndTimestamp - _inputBuffer.StartTimestamp) break;
                    float ratio = 0;
                    for (int k = _inputBuffer.UserBuffer.Count - 1; k >= 0; k--)
                    {
                        //Save reference for currently considered user input
                        NoteLine user = _inputBuffer.UserBuffer[k];
                        //Translate timestamps from global space to local buffer space
                        uint userLocalStart = _inputBuffer.EndTimestamp - user.StartTime;
                        uint userLocalEnd = user.EndTime == 0 ? 0 : _inputBuffer.EndTimestamp - user.EndTime;
                        //Check alignment conditions
                        if (user.Tone != melody.Tone || userLocalEnd > melodyLocalStart || userLocalStart < melodyLocalEnd) continue;
                        //Calculate alignment constrains
                        uint start = userLocalStart < melodyLocalStart ? userLocalStart : melodyLocalStart;
                        uint end = userLocalEnd > melodyLocalEnd ? userLocalEnd : melodyLocalEnd;
                        //Calculate alignment value
                        uint alignmentDistance = start - end;
                        float alignmentRatio = (alignmentDistance * 1.0f) / (melodyLocalStart - melodyLocalEnd);
                        if (ratio < alignmentRatio) ratio = alignmentRatio;
                    }
                    sum += ratio;
                    amount++;
                }
                float totalSegmentRatio = sum / amount;
                for (int j = 0; j < HighestAmount; j++)
                {
                    if (_highestRatios[j] < totalSegmentRatio)
                    {
                        float newRatio = totalSegmentRatio;
                        int newIndex = i;
                        for (int k = j; k < HighestAmount; k++)
                        {
                            float oldRatio = _highestRatios[k];
                            int oldIndex = _highestRatioIndices[k];
                            _highestRatios[k] = newRatio;
                            _highestRatioIndices[k] = newIndex;
                            newRatio = oldRatio;
                            newIndex = oldIndex;
                        }
                        break;
                    }
                }
            }
            int smallestDiff = int.MaxValue;
            for (int i = 0; i < HighestAmount; i++)
            {
                int diff = Math.Abs(_highestRatioIndices[i] - _previousHighestIndex);
                if (smallestDiff > diff && _highestRatios[i] >= 0 && _highestRatios[0] >= 0)
                {
                    float per = _highestRatios[i] / _highestRatios[0];
                    if (per >= MinimumPer)
                    {
                        highestRatioIndex = _highestRatioIndices[i];
                        smallestDiff = diff;
                    }
                }
                Console.WriteLine(@"Ratio: " + _highestRatios[i] + @" | Index: " + _highestRatioIndices[i]);
                Bindings.GetInstance().Report.WriteLine("Ratio: " + _highestRatios[i] + " | Index: " + _highestRatioIndices[i]);
            }
            
            _previousHighestIndex = highestRatioIndex;
            Console.WriteLine(@"Index: " + _previousHighestIndex + @" smallest diff: " + smallestDiff);
            Console.WriteLine(@"Function time: " + stopwatch.ElapsedTicks / (Stopwatch.Frequency / 1000f) + @"ms");
            Bindings.GetInstance().Report.WriteLine("Index: " + _previousHighestIndex + " smallest diff: " + smallestDiff);
            Bindings.GetInstance().Report.WriteLine("Function time: " + stopwatch.ElapsedTicks / (Stopwatch.Frequency / 1000f) + "ms");
            stopwatch.Stop();
            return _pieceBuffer.MelodyBuffer[highestRatioIndex].EndTime;
        }
    }
}