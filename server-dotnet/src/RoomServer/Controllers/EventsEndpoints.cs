using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using RoomServer.Services;
using System.Text.Json;
using System.Threading;

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

            var cancellationToken = context.RequestAborted;
            await context.Response.StartAsync(cancellationToken);

            var connectedEvent = new
            {
                type = "connected",
                source = "roomserver",
                timestamp = DateTime.UtcNow.ToString("O"),
                message = "Connected to RoomServer events"
            };

            var writeLock = new SemaphoreSlim(1, 1);

            await writeLock.WaitAsync(cancellationToken);
            try
            {
                await SendSseEventAsync(context.Response, connectedEvent, cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);
            }
            finally
            {
                writeLock.Release();
            }

            using var heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var heartbeatTask = RunHeartbeatAsync(context.Response, writeLock, heartbeatCts.Token);

            try
            {
                await foreach (var evt in eventPublisher.SubscribeAsync(cancellationToken))
                {
                    await writeLock.WaitAsync(cancellationToken);
                    try
                    {
                        await SendSseEventAsync(context.Response, evt, cancellationToken);
                        await context.Response.Body.FlushAsync(cancellationToken);
                    }
                    finally
                    {
                        writeLock.Release();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Client disconnected
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RoomServer SSE stream: {ex.Message}");
            }
            finally
            {
                heartbeatCts.Cancel();
                try
                {
                    await heartbeatTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected when the client disconnects
                }
            }
        });
    }

    private static async Task SendSseEventAsync(HttpResponse response, object data, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await response.WriteAsync($"data: {json}\n\n", cancellationToken);
    }

    private static async Task RunHeartbeatAsync(HttpResponse response, SemaphoreSlim writeLock, CancellationToken cancellationToken)
    {
        try
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                await writeLock.WaitAsync(cancellationToken);
                try
                {
                    await response.WriteAsync(": ping\n\n", cancellationToken);
                    await response.Body.FlushAsync(cancellationToken);
                }
                finally
                {
                    writeLock.Release();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the client disconnects
        }
    }
}
