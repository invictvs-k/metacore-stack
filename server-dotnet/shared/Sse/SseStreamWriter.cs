using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Metacore.Shared.Sse;

public sealed class SseStreamWriter : IAsyncDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(10);

    private readonly HttpResponse _response;
    private readonly ILogger? _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly TimeSpan _heartbeatInterval;

    private CancellationTokenSource? _heartbeatCts;
    private Task? _heartbeatTask;

    public SseStreamWriter(HttpResponse response, ILogger? logger = null, TimeSpan? heartbeatInterval = null)
    {
        _response = response;
        _logger = logger;
        _heartbeatInterval = heartbeatInterval ?? DefaultHeartbeatInterval;
    }

    public static void ConfigureResponse(HttpResponse response)
    {
        response.ContentType = "text/event-stream; charset=utf-8";
        response.Headers["Cache-Control"] = "no-cache";
        response.Headers["X-Accel-Buffering"] = "no";
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _response.StartAsync(cancellationToken).ConfigureAwait(false);

        _heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _heartbeatTask = RunHeartbeatAsync(_heartbeatCts.Token);
    }

    public Task WriteEventAsync(object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        return WriteRawAsync($"data: {json}\n\n", cancellationToken);
    }

    private async Task WriteRawAsync(string payload, CancellationToken cancellationToken)
    {
        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _response.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
            await _response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task RunHeartbeatAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_heartbeatInterval, cancellationToken).ConfigureAwait(false);
                await WriteRawAsync(": ping\n\n", cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the client disconnects or the stream is disposed.
        }
        catch (Exception ex)
        {
            _logger?.LogDebug(ex, "SSE heartbeat loop terminated unexpectedly.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_heartbeatCts != null)
        {
            try
            {
                _heartbeatCts.Cancel();
                if (_heartbeatTask != null)
                {
                    try
                    {
                        await _heartbeatTask.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when the heartbeat is cancelled.
                    }
                }
            }
            finally
            {
                _heartbeatCts.Dispose();
            }
        }

        _writeLock.Dispose();
    }
}
