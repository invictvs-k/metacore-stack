using System;
using System.Collections.Concurrent;
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

    public (Guid SubscriptionId, ChannelReader<T> Reader, ChannelWriter<T> Writer) CreateSubscription()
    {
        var channel = Channel.CreateBounded<T>(_optionsFactory());
        var subscriptionId = Guid.NewGuid();

        _subscribers[subscriptionId] = channel.Writer;

        return (subscriptionId, channel.Reader, channel.Writer);
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
}
