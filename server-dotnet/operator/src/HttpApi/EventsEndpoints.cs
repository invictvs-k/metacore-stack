using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RoomOperator.Core;
using System.Text.Json;

namespace RoomOperator.HttpApi;

public static class EventsEndpoints
{
    private const int DefaultHeartbeatIntervalMs = 10000;

    public static void MapEventsEndpoints(this WebApplication app)
    {
        // GET /events - Server-Sent Events endpoint
        app.MapGet("/events", async (
            HttpContext context, 
            RoomOperatorService operatorService,
            IConfiguration configuration,
            ILogger<Program> logger) =>
        {
            context.Response.Headers["Content-Type"] = "text/event-stream";
            context.Response.Headers["Cache-Control"] = "no-cache";
            context.Response.Headers["Connection"] = "keep-alive";
            context.Response.Headers["X-Accel-Buffering"] = "no";

            // Send initial connected event
            var connectedEvent = new
            {
                type = "connected",
                source = "roomoperator",
                timestamp = DateTime.UtcNow.ToString("O"),
                message = "Connected to RoomOperator events"
            };
            
            await SendSseEventAsync(context.Response, connectedEvent);
            await context.Response.Body.FlushAsync();

            // Get configurable heartbeat interval
            var heartbeatIntervalMs = configuration.GetValue<int?>("Events:HeartbeatIntervalMs") ?? DefaultHeartbeatIntervalMs;
            
            // Keep connection alive with periodic heartbeats
            var cancellationToken = context.RequestAborted;
            
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Send heartbeat comment (keeps connection alive)
                    await context.Response.WriteAsync(": ping\n\n");
                    await context.Response.Body.FlushAsync();
                    
                    // Wait before next heartbeat
                    await Task.Delay(heartbeatIntervalMs, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected, this is expected
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in SSE stream");
            }
        });
    }

    private static async Task SendSseEventAsync(HttpResponse response, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await response.WriteAsync($"data: {json}\n\n");
    }
}
