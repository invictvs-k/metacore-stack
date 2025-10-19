using System.Threading.Channels;

namespace Metacore.Shared.Channels;

public static class ChannelSettings
{
    public const int DefaultCapacity = 256;

    public static BoundedChannelOptions CreateSingleReaderOptions(
        int capacity = DefaultCapacity,
        BoundedChannelFullMode fullMode = BoundedChannelFullMode.DropOldest)
    {
        return new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            AllowSynchronousContinuations = false,
            FullMode = fullMode
        };
    }
}
