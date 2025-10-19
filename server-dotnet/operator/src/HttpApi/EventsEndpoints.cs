using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RoomOperator.Core;
using Metacore.Shared.Sse;

namespace RoomOperator.HttpApi;

public static class EventsEndpoints
{
    private const int DefaultHeartbeatIntervalMs = 10000;

    public static void MapEventsEndpoints(this WebApplication app)
    {
        // GET /events - Server-Sent Events endpoint
        app.MapGet("/events", async (
            HttpContext context,
            AuditLog auditLog,
            ILogger<AuditLog> logger) =>
        {
            SseStreamWriter.ConfigureResponse(context.Response);
            var cancellationToken = context.RequestAborted;

            await using var sse = new SseStreamWriter(context.Response, logger);
            await sse.StartAsync(cancellationToken);

            var connectedEvent = new
            {
                type = "connected",
                source = "roomoperator",
                timestamp = DateTime.UtcNow.ToString("O"),
                message = "Connected to RoomOperator events"
            };

            await sse.WriteEventAsync(
                connectedEvent,
                eventName: "connected",
                cancellationToken: cancellationToken);

            try
            {
                await foreach (var entry in auditLog.SubscribeAsync(cancellationToken: cancellationToken))
                {
                    await sse.WriteEventAsync(
                        entry,
                        eventName: entry.Type,
                        eventId: string.IsNullOrWhiteSpace(entry.CorrelationId) ? null : entry.CorrelationId,
                        cancellationToken: cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error streaming RoomOperator events.");
            }
        });
    }
}
