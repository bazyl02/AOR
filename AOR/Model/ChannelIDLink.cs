namespace AOR.Model
{
    public class ChannelIdLink
    {
        public readonly bool UsesChannel;
        public readonly int DeviceId;
        public readonly int ChannelId;

        public ChannelIdLink(bool usesChannel, int deviceId, int channelId)
        {
            UsesChannel = usesChannel;
            DeviceId = deviceId;
            ChannelId = channelId;
        }
    }
}