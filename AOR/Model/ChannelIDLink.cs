
namespace AOR.Model
{
    public class ChannelIDLink
    {
        public bool UsesChannel = false;
        public int DeviceId;
        public int ChannelId;

        public ChannelIDLink(bool usesChannel, int deviceId, int channelId)
        {
            UsesChannel = usesChannel;
            DeviceId = deviceId;
            ChannelId = channelId;
        }
    }
}