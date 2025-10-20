using System.Threading.Channels;

namespace Metacore.Shared.Channels;

public static class ChannelSettings
{
  /// <summary>
  /// Default bounded channel capacity used across server-side SSE publishers.
  /// </summary>
  public const int DefaultCapacity = 256;

  /// <summary>
  /// Creates <see cref="BoundedChannelOptions"/> for a single-reader channel.
  /// </summary>
  /// <remarks>
  /// The default <paramref name="fullMode"/> is <see cref="BoundedChannelFullMode.DropOldest"/>,
  /// which will discard the oldest buffered message when the channel is full. This trade-off favors
  /// keeping the most recent events flowing for SSE clients at the cost of potentially losing older
  /// entries when downstream consumers are slow. Call sites should pass an explicit <paramref name="fullMode"/>
  /// if a different behavior is required so the intent is clear.
  /// </remarks>
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
