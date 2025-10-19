using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using NUlid;
using RoomServer.Hubs;
using RoomServer.Models;
using RoomServer.Services.ArtifactStore;

namespace RoomServer.Services;

public class RoomEventPublisher
{
  private readonly IHubContext<RoomHub> _hubContext;
  private readonly RoomObservabilityService _observability;
  private readonly ConcurrentDictionary<Guid, ChannelWriter<object>> _subscribers = new();

  public RoomEventPublisher(IHubContext<RoomHub> hubContext, RoomObservabilityService observability)
  {
    _hubContext = hubContext;
    _observability = observability;
  }

  public IAsyncEnumerable<object> SubscribeAsync(CancellationToken cancellationToken = default)
  {
    var channel = Channel.CreateUnbounded<object>(new UnboundedChannelOptions
    {
      SingleReader = true,
      AllowSynchronousContinuations = false
    });

    var subscriptionId = Guid.NewGuid();
    _subscribers[subscriptionId] = channel.Writer;

    var registration = cancellationToken.Register(() => CompleteSubscription(subscriptionId));

    return ReadAllAsync(channel.Reader, subscriptionId, registration, cancellationToken);
  }

  public async Task PublishAsync(string roomId, string eventType, object data)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(roomId);
    ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

    var eventPayload = new
    {
      id = Ulid.NewUlid().ToString(),
      roomId,
      type = "event",
      payload = new { kind = eventType, data },
      ts = DateTime.UtcNow
    };

    // Log to events.jsonl
    await _observability.LogEventAsync(roomId, eventType, data);

    // Broadcast to SignalR clients
    await _hubContext.Clients.Group(roomId).SendAsync("event", eventPayload);

    Broadcast(eventPayload);
  }

  public async Task PublishArtifactMessageAsync(string roomId, ArtifactManifest manifest, string from = "E-SERVER", string channel = "room")
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(roomId);
    ArgumentNullException.ThrowIfNull(manifest);

    var message = new MessageModel
    {
      Id = Ulid.NewUlid().ToString(),
      RoomId = roomId,
      Channel = channel,
      From = from,
      Type = "artifact",
      Payload = new { manifest },
      Ts = DateTime.UtcNow
    };

    // Track artifact in stats
    _observability.TrackArtifact(roomId);

    await _hubContext.Clients.Group(roomId).SendAsync("message", message);

    Broadcast(message);
  }

  private async IAsyncEnumerable<object> ReadAllAsync(
      ChannelReader<object> reader,
      Guid subscriptionId,
      CancellationTokenRegistration registration,
      [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    try
    {
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
      registration.Dispose();
      CompleteSubscription(subscriptionId);
    }
  }

  private void Broadcast(object payload)
  {
    foreach (var kvp in _subscribers)
    {
      var subscriptionId = kvp.Key;
      var writer = kvp.Value;

      if (!writer.TryWrite(payload))
      {
        CompleteSubscription(subscriptionId);
      }
    }
  }

  private void CompleteSubscription(Guid subscriptionId)
  {
    if (_subscribers.TryRemove(subscriptionId, out var writer))
    {
      writer.TryComplete();
    }
  }
}
