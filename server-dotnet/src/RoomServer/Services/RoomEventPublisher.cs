using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR;
using NUlid;
using RoomServer.Hubs;
using RoomServer.Models;
using RoomServer.Services.ArtifactStore;
using Metacore.Shared.Channels;

namespace RoomServer.Services;

public class RoomEventPublisher
{
  private readonly IHubContext<RoomHub> _hubContext;
  private readonly RoomObservabilityService _observability;
  private readonly ChannelSubscriptionManager<RoomEventStreamItem> _subscriptions;

  public RoomEventPublisher(IHubContext<RoomHub> hubContext, RoomObservabilityService observability)
  {
    _hubContext = hubContext;
    _observability = observability;
    _subscriptions = new ChannelSubscriptionManager<RoomEventStreamItem>(() => ChannelSettings.CreateSingleReaderOptions());
  }

  public IAsyncEnumerable<RoomEventStreamItem> SubscribeAsync(CancellationToken cancellationToken = default)
  {
    var (subscriptionId, reader, _) = _subscriptions.CreateSubscription();

    return ReadAllAsync(subscriptionId, reader, cancellationToken);
  }

  public async Task PublishAsync(string roomId, string eventType, object data)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(roomId);
    ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

    var eventPayload = new RoomEventStreamNotification
    {
      Id = Ulid.NewUlid().ToString(),
      RoomId = roomId,
      Timestamp = DateTime.UtcNow,
      Payload = new RoomEventPayload
      {
        Kind = eventType,
        Data = data
      }
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

    var envelope = new RoomEventStreamMessage
    {
      Id = message.Id,
      RoomId = message.RoomId,
      Timestamp = message.Ts,
      Payload = message
    };

    Broadcast(envelope);
  }

  private async IAsyncEnumerable<RoomEventStreamItem> ReadAllAsync(
      Guid subscriptionId,
      ChannelReader<RoomEventStreamItem> reader,
      [EnumeratorCancellation] CancellationToken cancellationToken)
  {
    using var registration = cancellationToken.Register(() => _subscriptions.Complete(subscriptionId));
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
      _subscriptions.Complete(subscriptionId);
    }
  }

  private void Broadcast(RoomEventStreamItem payload) => _subscriptions.Broadcast(payload);
}
