//#define ShowRatioArray

using System;
using System.Diagnostics;
using AOR.ModelView;

namespace AOR.Model
{
    public class Algorithm
    {
        private const float MinimumPer = 0.9f;
        private const int HighestAmount = 10;

        private const int FrontSize = 64;
        private const int BehindSize = 64;

        private const int GraceValue = 4;

        private const float SpeedStep = 0.05f;
        
        private readonly PieceBuffer _pieceBuffer = Bindings.GetInstance().PieceBuffer;
        private readonly InputBuffer _inputBuffer = Bindings.GetInstance().InputBuffer;

        private int _previousHighestIndex = -1;
        private readonly float[,] _highestRatios = new float[HighestAmount, 3];
        private readonly int[,] _highestRatioIndices = new int[HighestAmount , 3];
        
        private float _previousSpeed = 1.0f;

        public uint Run()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int highestRatioIndex = 0;
            for (int i = 0; i < HighestAmount; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    _highestRatios[i, j] = -1.0f;
                }
            }

            int startIndex = Math.Max(_previousHighestIndex - BehindSize, 0);
            int endIndex = Math.Min(_previousHighestIndex + FrontSize, _pieceBuffer.MelodyBuffer.Count);

            for (int step = -1; step <= 1; step++)
            {
                float checkedSpeed = _previousSpeed + SpeedStep * step;
                for (int i = startIndex; i < endIndex; i++)
                {
                    float startTime = _inputBuffer.StartTimestamp * checkedSpeed;
                    float endTime = _inputBuffer.EndTimestamp * checkedSpeed; 
                    uint bufferTime = _pieceBuffer.MelodyBuffer[i].EndTime;
                    if (bufferTime < endTime - startTime) continue;
                    float sum = 0;
                    int amount = 0;
                    for (int j = i; j >= 0; j--)
                    {
                        NoteLine melody = _pieceBuffer.MelodyBuffer[j];
                        //Translate timestamps from global space to local buffer space
                        uint melodyLocalStart = bufferTime - melody.StartTime;
                        uint melodyLocalEnd = melody.EndTime > bufferTime ? 0 : bufferTime - melody.EndTime;
                        if (melodyLocalStart > endTime - startTime - GraceValue) break;
                        float ratio = 0;
                        for (int k = _inputBuffer.UserBuffer.Count - 1; k >= 0; k--)
                        {
                            //Save reference for currently considered user input
                            NoteLine user = _inputBuffer.UserBuffer[k];
                            //Translate timestamps from global space to local buffer space
                            float userLocalStart = endTime - (user.StartTime * checkedSpeed);
                            float userLocalEnd = user.EndTime == 0 ? 0 : endTime - (user.EndTime * checkedSpeed);
                            //Check alignment conditions
                            if (user.Tone != melody.Tone || userLocalEnd > melodyLocalStart ||
                                userLocalStart < melodyLocalEnd) continue;
                            //Calculate alignment constrains
                            float start = userLocalStart < melodyLocalStart ? userLocalStart : melodyLocalStart;
                            float end = userLocalEnd > melodyLocalEnd ? userLocalEnd : melodyLocalEnd;
                            //Calculate alignment value
                            float alignmentDistance = start - end;
                            float alignmentRatio = alignmentDistance / (melodyLocalStart - melodyLocalEnd);
                            if (ratio < alignmentRatio) ratio = alignmentRatio;
                        }
                        sum += ratio;
                        amount++;
                    }
    
                    float totalSegmentRatio = sum / amount;
                    for (int j = 0; j < HighestAmount; j++)
                    {
                        if (_highestRatios[j,step + 1] < totalSegmentRatio)
                        {
                            float newRatio = totalSegmentRatio;
                            int newIndex = i;
                            for (int k = j; k < HighestAmount; k++)
                            {
                                float oldRatio = _highestRatios[k ,step + 1];
                                int oldIndex = _highestRatioIndices[k ,step + 1];
                                _highestRatios[k ,step + 1] = newRatio;
                                _highestRatioIndices[k ,step + 1] = newIndex;
                                newRatio = oldRatio;
                                newIndex = oldIndex;
                            }
    
                            break;
                        }
                    }
                }
            }

            float highest = -1;
            int highestIndex = -1;
            for (int i = 0; i < 3; i++)
            {
                if (_highestRatios[0, i] > highest)
                {
                    highest = _highestRatios[0, i];
                    highestIndex = i;
                }
            }
            _previousSpeed += SpeedStep * (highestIndex - 1);
            Console.WriteLine(@"New Speed: " + _previousSpeed);
            
            int smallestDiff = int.MaxValue;
            for (int i = 0; i < HighestAmount; i++)
            {
                int diff = Math.Abs(_highestRatioIndices[i,highestIndex] - _previousHighestIndex);
                if (smallestDiff > diff && _highestRatios[i,highestIndex] >= 0 && _highestRatios[0,highestIndex] >= 0)
                {
                    float per = _highestRatios[i,highestIndex] / _highestRatios[0,highestIndex];
                    if (per >= MinimumPer)
                    {
                        highestRatioIndex = _highestRatioIndices[i,highestIndex];
                        smallestDiff = diff;
                    }
                }
#if ShowRatioArray
                Console.WriteLine(@"Ratio: " + _highestRatios[i] + @" | Index: " + _highestRatioIndices[i]);
#endif
#if DUMP
                Bindings.GetInstance().Report.WriteLine("Ratio: " + _highestRatios[i] + " | Index: " + _highestRatioIndices[i]);
#endif
            }
            
            _previousHighestIndex = highestRatioIndex;
            Console.WriteLine(@"Index: " + _previousHighestIndex + @" smallest diff: " + smallestDiff);
            Console.WriteLine(@"Function time: " + stopwatch.ElapsedTicks / (Stopwatch.Frequency / 1000f) + @"ms");
#if DUMP
            Bindings.GetInstance().Report.WriteLine("Index: " + _previousHighestIndex + " smallest diff: " + smallestDiff);
            Bindings.GetInstance().Report.WriteLine("Function time: " + stopwatch.ElapsedTicks / (Stopwatch.Frequency / 1000f) + "ms");
#endif
            stopwatch.Stop();
            return _pieceBuffer.MelodyBuffer[highestRatioIndex].EndTime;
        }
    }
}