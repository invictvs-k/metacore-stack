using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;

namespace Metacore.Shared.Channels;

public sealed class ChannelSubscriptionManager<T>
{
  private readonly ConcurrentDictionary<Guid, ChannelWriter<T>> _subscribers = new();
  private readonly Func<BoundedChannelOptions> _optionsFactory;

  public ChannelSubscriptionManager(Func<BoundedChannelOptions> optionsFactory)
  {
    _optionsFactory = optionsFactory ?? throw new ArgumentNullException(nameof(optionsFactory));
  }

  private (Guid SubscriptionId, ChannelReader<T> Reader) CreateSubscription()
  {
    var channel = Channel.CreateBounded<T>(_optionsFactory());
    var subscriptionId = Guid.NewGuid();

    _subscribers[subscriptionId] = channel.Writer;

    return (subscriptionId, channel.Reader);
  }

  public IAsyncEnumerable<T> SubscribeAsync(
      Func<CancellationToken, IAsyncEnumerable<T>>? replayFactory = null,
      CancellationToken cancellationToken = default)
  {
    var (subscriptionId, reader) = CreateSubscription();

    return ReadAllAsync(subscriptionId, reader, replayFactory, cancellationToken);
  }

  public void Broadcast(T item)
  {
    foreach (var (subscriptionId, writer) in _subscribers)
    {
      if (!writer.TryWrite(item))
      {
        Complete(subscriptionId);
      }
    }
  }

  public void Complete(Guid subscriptionId)
  {
    if (_subscribers.TryRemove(subscriptionId, out var writer))
    {
      writer.TryComplete();
    }
  }

  private async IAsyncEnumerable<T> ReadAllAsync(
      Guid subscriptionId,
      ChannelReader<T> reader,
      Func<CancellationToken, IAsyncEnumerable<T>>? replayFactory,
      [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    using var registration = cancellationToken.Register(() => Complete(subscriptionId));

    try
    {
      if (replayFactory is not null)
      {
        var replay = replayFactory(cancellationToken);

        await foreach (var item in replay.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
          yield return item;
        }
      }

      while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
      {
        while (reader.TryRead(out var item))
        {
          yield return item;
        }
      }
    }
    finally
    {
      Complete(subscriptionId);
    }
  }
}
