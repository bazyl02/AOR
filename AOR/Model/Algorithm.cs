using System;
using System.Collections.Generic;
using System.Diagnostics;
using AOR.ModelView;

namespace AOR.Model
{
    public class Algorithm
    {
        private PieceBuffer _pieceBuffer;
        private InputBuffer _inputBuffer;

        private int _previousHighestIndex = 0;
        private float[] _highestRatios = new float[3];
        private int[] _highestRatioIndices = new int[3];
        public Algorithm()
        {
            _pieceBuffer = Bindings.GetInstance().PieceBuffer;
            _inputBuffer = Bindings.GetInstance().InputBuffer;
        }
        
        public uint Run()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            float highestRatio = 0;
            int highestRatioIndex = 0;
            for (int i = 0; i < 3; i++)
            {
                _highestRatios[i] = -1.0f;
            }
            
            for (int i = 0; i < _pieceBuffer.MelodyBuffer.Count; i++)
            {
                uint bufferTime = _pieceBuffer.MelodyBuffer[i].EndTime;
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
                //if (highestRatio < totalSegmentRatio)
                //{
                //    highestRatio = totalSegmentRatio;
                //    highestRatioIndex = i;
                //}

                for (int j = 0; j < 3; j++)
                {
                    if (_highestRatios[j] < totalSegmentRatio)
                    {
                        _highestRatios[j] = totalSegmentRatio;
                        _highestRatioIndices[j] = i;
                        break;
                    }
                }
            }

            int smallestDiff = int.MaxValue;
            for (int i = 0; i < 3; i++)
            {
                int diff = Math.Abs(_highestRatioIndices[i] - _previousHighestIndex);
                if (smallestDiff > diff && _highestRatios[i] >= 0)
                {
                    highestRatioIndex = _highestRatioIndices[i];
                    smallestDiff = diff;
                }
            }
            
            _previousHighestIndex = highestRatioIndex;
            Console.WriteLine(@"Index: " + _previousHighestIndex + @" smallest diff: " + smallestDiff);
            Console.WriteLine(@"Function time: " + stopwatch.ElapsedTicks / (Stopwatch.Frequency / 1000f) + @"ms");
            stopwatch.Stop();
            return _pieceBuffer.MelodyBuffer[highestRatioIndex].EndTime;
        }
    }
}