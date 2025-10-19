using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using RoomServer.Services;
using System.Text;
using System.Text.Json;

namespace RoomServer.Controllers;

public static class EventsEndpoints
{
    public static void MapEventsEndpoints(this WebApplication app)
    {
        // GET /events - Server-Sent Events endpoint
        app.MapGet("/events", async (HttpContext context, RoomEventPublisher eventPublisher) =>
        {
            context.Response.Headers["Content-Type"] = "text/event-stream";
            context.Response.Headers["Cache-Control"] = "no-cache";
            context.Response.Headers["Connection"] = "keep-alive";
            context.Response.Headers["X-Accel-Buffering"] = "no";

            // Send initial connected event
            var connectedEvent = new
            {
                type = "connected",
                source = "roomserver",
                timestamp = DateTime.UtcNow.ToString("O"),
                message = "Connected to RoomServer events"
            };
            
            await SendSseEventAsync(context.Response, connectedEvent);
            await context.Response.Body.FlushAsync();

            // Keep connection alive with periodic heartbeats
            var cancellationToken = context.RequestAborted;
            
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Send heartbeat comment (keeps connection alive)
                    await context.Response.WriteAsync(": ping\n\n");
                    await context.Response.Body.FlushAsync();
                    
                    // Wait 10 seconds before next heartbeat
                    await Task.Delay(10000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected, this is expected
            }
            catch (Exception ex)
            {
                // Log error but don't throw
                Console.WriteLine($"Error in SSE stream: {ex.Message}");
            }
        });
    }

    private static async Task SendSseEventAsync(HttpResponse response, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await response.WriteAsync($"data: {json}\n\n");
    }
}
