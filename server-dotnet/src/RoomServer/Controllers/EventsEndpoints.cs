using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RoomServer.Services;
using Metacore.Shared.Sse;

namespace RoomServer.Controllers;

public static class EventsEndpoints
{
  private const int DefaultHeartbeatIntervalMs = 10000;

  public static void MapEventsEndpoints(this WebApplication app)
  {
    // GET /events - Server-Sent Events endpoint
    app.MapGet("/events", async (
        HttpContext context,
        RoomEventPublisher eventPublisher,
        ILogger<RoomEventPublisher> logger) =>
    {
      SseStreamWriter.ConfigureResponse(context.Response);
      var cancellationToken = context.RequestAborted;

      await using var sse = new SseStreamWriter(context.Response, logger);
      await sse.StartAsync(cancellationToken);

      var connectedEvent = new
      {
        type = "connected",
        source = "roomserver",
        timestamp = DateTime.UtcNow.ToString("O"),
        message = "Connected to RoomServer events"
      };

      await sse.WriteEventAsync(
              connectedEvent,
              eventName: "connected",
              cancellationToken: cancellationToken);

      try
      {
        await foreach (var evt in eventPublisher.SubscribeAsync(cancellationToken))
        {
          await sse.WriteEventAsync(
                  evt,
                  eventName: evt.Type,
                  eventId: evt.Id,
                  cancellationToken: cancellationToken);
        }
      }
      catch (OperationCanceledException)
      {
        // Client disconnected
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "Error streaming RoomServer events.");
      }
    });
  }
}
