using System.Collections.Generic;
using Melanchall.DryWetMidi.Common;

namespace AOR.Model
{
    public static class Helpers
    {
        public static List<byte> GetChannels(IEnumerable<FourBitNumber> channels)
        {
            List<byte> output = new List<byte>();
            foreach (var channel in channels)
            {
                output.Add(channel);
            }
            return output;
        }
    }
}