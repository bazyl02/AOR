using AOR.ModelView;

namespace AOR.Model
{
    public class Algorithm
    {
        private PieceBuffer _pieceBuffer;
        private InputBuffer _inputBuffer;

        private int _previousHighestIndex = 0;

        public Algorithm()
        {
            _pieceBuffer = Bindings.GetInstance().PieceBuffer;
            _inputBuffer = Bindings.GetInstance().InputBuffer;
        }

        public uint Run()
        {
            float highestRatio = 0;
            int highestRatioIndex = 0;
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
                    uint melodyLocalEnd = bufferTime - melody.EndTime;
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
                        if (user.Tone != melody.Tone || userLocalEnd > melodyLocalStart ||  userLocalStart < melodyLocalEnd) continue;
                        //Calculate alignment constains
                        uint start = userLocalStart < melodyLocalStart ? userLocalStart : melodyLocalStart;
                        uint end = userLocalEnd > melodyLocalEnd ? userLocalEnd : melodyLocalEnd;
                        //Calculate alignment value
                        uint alignmentDistance = start - end;
                        float alignmentRatio = (melody.EndTime - melody.StartTime) / (alignmentDistance * 1.0f);
                        if (ratio < alignmentRatio) ratio = alignmentRatio;
                    }
                    sum += ratio;
                    amount++;
                }
                float totalSegmentRatio = sum / amount;
                if (highestRatio < totalSegmentRatio)
                {
                    highestRatio = totalSegmentRatio;
                    highestRatioIndex = i;
                }
            }
            _previousHighestIndex = highestRatioIndex;
            return _pieceBuffer.MelodyBuffer[highestRatioIndex].EndTime;
        }
    }
}