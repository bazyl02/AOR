using System.Collections.Generic;

namespace AOR.Model
{
    public class InputDeviceData
    {
        public string Name;
        public int Offset;
        public bool UsesChannels = false;
        public Dictionary<int, int> ChannelsOffsets = new Dictionary<int, int>();

        public InputDeviceData(string name, int offset, bool usesChannels)
        {
            Name = name;
            Offset = offset;
            UsesChannels = usesChannels;
        }
    }
}