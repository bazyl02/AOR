using System.Collections.Generic;

namespace AOR.Model
{
    public class InputDeviceData
    {
        public int Offset;
        public bool UsesChannels = false;
        public Dictionary<int, int> ChannelsOffsets = new Dictionary<int, int>();

        public InputDeviceData(int offset, bool usesChannels)
        {
            Offset = offset;
            UsesChannels = usesChannels;
        }
    }
}