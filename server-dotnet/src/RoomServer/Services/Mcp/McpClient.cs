using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RoomServer.Models;

namespace RoomServer.Services.Mcp;

/// <summary>
/// WebSocket-based JSON-RPC 2.0 client for communicating with MCP servers.
/// Implements automatic reconnection with exponential backoff.
/// </summary>
public class McpClient : IMcpClient, IDisposable
{
    private readonly string _url;
    private readonly ILogger<McpClient> _logger;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement>> _pendingRequests = new();
    private readonly CancellationTokenSource _lifecycleCts = new();
    
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _connectionCts;
    private Task? _receiveTask;
    private int _nextRequestId = 1;
    private ToolSpec[]? _cachedTools;
    private int _reconnectAttempts = 0;
    private const int MaxReconnectDelaySeconds = 60;
    private bool _disposed = false;

    public string ServerId { get; }
    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    public McpClient(string serverId, string url, ILogger<McpClient> logger)
    {
        ServerId = serverId ?? throw new ArgumentNullException(nameof(serverId));
        _url = url ?? throw new ArgumentNullException(nameof(url));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ConnectAsync()
    {
        if (IsConnected)
        {
            _logger.LogDebug("[{ServerId}] Already connected", ServerId);
            return;
        }

        await DisconnectAsync();

        _connectionCts = new CancellationTokenSource();
        _webSocket = new ClientWebSocket();

        try
        {
            _logger.LogInformation("[{ServerId}] Connecting to {Url}", ServerId, _url);
            await _webSocket.ConnectAsync(new Uri(_url), _connectionCts.Token);
            _reconnectAttempts = 0;
            _logger.LogInformation("[{ServerId}] Connected successfully", ServerId);

            _receiveTask = Task.Run(() => ReceiveLoopAsync(_connectionCts.Token));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServerId}] Failed to connect", ServerId);
            // Schedule reconnection in background without blocking
            _ = Task.Run(async () =>
            {
                try
                {
                    await ScheduleReconnectAsync();
                }
                catch (Exception reconnectEx)
                {
                    _logger.LogError(reconnectEx, "[{ServerId}] Error during reconnection", ServerId);
                }
            });
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        _connectionCts?.Cancel();
        
        if (_webSocket != null)
        {
            if (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived)
            {
                try
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[{ServerId}] Error during disconnect", ServerId);
                }
            }
            _webSocket.Dispose();
            _webSocket = null;
        }

        if (_receiveTask != null)
        {
            try
            {
                await _receiveTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelling
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[{ServerId}] Error waiting for receive task", ServerId);
            }
            _receiveTask = null;
        }

        _connectionCts?.Dispose();
        _connectionCts = null;

        // Cancel all pending requests
        foreach (var tcs in _pendingRequests.Values)
        {
            tcs.TrySetCanceled();
        }
        _pendingRequests.Clear();
        _cachedTools = null;
    }

    public async Task<ToolSpec[]> ListToolsAsync()
    {
        if (_cachedTools != null)
        {
            return _cachedTools;
        }

        var request = new
        {
            jsonrpc = "2.0",
            id = Interlocked.Increment(ref _nextRequestId),
            method = "tools/list",
            @params = new { }
        };

        var response = await SendRequestAsync(request);
        
        // Check for JSON-RPC error response
        if (response.TryGetProperty("error", out var error))
        {
            var code = error.TryGetProperty("code", out var codeElement) ? codeElement.GetInt32() : -32000;
            var message = error.TryGetProperty("message", out var msgElement) ? msgElement.GetString() : "Unknown error";
            throw new McpServerException(code, message ?? "Unknown error");
        }
        
        if (response.TryGetProperty("result", out var result) && 
            result.TryGetProperty("tools", out var toolsArray))
        {
            _cachedTools = JsonSerializer.Deserialize<ToolSpec[]>(toolsArray.GetRawText()) ?? Array.Empty<ToolSpec>();
            _logger.LogInformation("[{ServerId}] Cached {Count} tools", ServerId, _cachedTools.Length);
            return _cachedTools;
        }

        throw new InvalidOperationException($"[{ServerId}] Invalid response format from tools/list");
    }

    public async Task<JsonElement> CallToolRawAsync(string toolId, JsonElement input)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException($"[{ServerId}] Not connected to MCP server");
        }

        var request = new
        {
            jsonrpc = "2.0",
            id = Interlocked.Increment(ref _nextRequestId),
            method = "tool/call",
            @params = new
            {
                toolId,
                input
            }
        };

        var response = await SendRequestAsync(request);

        if (response.TryGetProperty("error", out var error))
        {
            var code = error.TryGetProperty("code", out var codeElement) ? codeElement.GetInt32() : -32000;
            var message = error.TryGetProperty("message", out var msgElement) ? msgElement.GetString() : "Unknown error";
            throw new McpServerException(code, message ?? "Unknown error");
        }

        if (response.TryGetProperty("result", out var result))
        {
            return result;
        }

        throw new InvalidOperationException($"[{ServerId}] Invalid response format from tool/call");
    }

    private async Task<JsonElement> SendRequestAsync(object request)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException($"[{ServerId}] Not connected to MCP server");
        }

        var requestJson = JsonSerializer.Serialize(request);
        var requestElement = JsonSerializer.Deserialize<JsonElement>(requestJson);
        
        if (!requestElement.TryGetProperty("id", out var idElement))
        {
            throw new InvalidOperationException("Request must have an 'id' property");
        }
        
        var requestId = idElement.GetInt32();

        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[requestId] = tcs;

        try
        {
            await _sendLock.WaitAsync();
            try
            {
                var bytes = Encoding.UTF8.GetBytes(requestJson);
                await _webSocket!.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    endOfMessage: true,
                    _connectionCts?.Token ?? CancellationToken.None);
                
                _logger.LogDebug("[{ServerId}] Sent request {RequestId}", ServerId, requestId);
            }
            finally
            {
                _sendLock.Release();
            }

            return await tcs.Task;
        }
        catch
        {
            _pendingRequests.TryRemove(requestId, out var _);
            throw;
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        var messageBuilder = new MemoryStream();

        try
        {
            while (!cancellationToken.IsCancellationRequested && _webSocket != null)
            {
                messageBuilder.SetLength(0);
                WebSocketReceiveResult? result;

                do
                {
                    result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                    
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _logger.LogWarning("[{ServerId}] WebSocket closed by server", ServerId);
                        await ScheduleReconnectAsync();
                        return;
                    }

                    messageBuilder.Write(buffer, 0, result.Count);
                } while (!result.EndOfMessage);

                var messageText = Encoding.UTF8.GetString(messageBuilder.ToArray());
                ProcessMessage(messageText);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("[{ServerId}] Receive loop cancelled", ServerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServerId}] Error in receive loop", ServerId);
            await ScheduleReconnectAsync();
        }
    }

    private void ProcessMessage(string messageText)
    {
        try
        {
            var response = JsonSerializer.Deserialize<JsonElement>(messageText);
            
            if (response.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.Number)
            {
                var id = idElement.GetInt32();
                if (_pendingRequests.TryRemove(id, out var tcs))
                {
                    tcs.TrySetResult(response);
                    _logger.LogDebug("[{ServerId}] Completed request {RequestId}", ServerId, id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServerId}] Error processing message: {Message}", ServerId, messageText);
        }
    }

    private async Task ScheduleReconnectAsync()
    {
        var delaySeconds = Math.Min(Math.Pow(2, _reconnectAttempts), MaxReconnectDelaySeconds);
        _reconnectAttempts++;
        
        _logger.LogInformation("[{ServerId}] Scheduling reconnect attempt {Attempt} in {Delay}s", 
            ServerId, _reconnectAttempts, delaySeconds);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), _lifecycleCts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("[{ServerId}] Reconnect cancelled", ServerId);
            return;
        }

        // Check if disposed or cancelled before attempting reconnect
        if (_disposed || _lifecycleCts.IsCancellationRequested)
        {
            _logger.LogDebug("[{ServerId}] Client disposed, skipping reconnect", ServerId);
            return;
        }

        try
        {
            await ConnectAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ServerId}] Reconnect attempt {Attempt} failed", ServerId, _reconnectAttempts);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _lifecycleCts.Cancel();
        
        DisconnectAsync().GetAwaiter().GetResult();
        
        _sendLock.Dispose();
        _lifecycleCts.Dispose();
    }
}

/// <summary>
/// Exception thrown when an MCP server returns an error response.
/// </summary>
public class McpServerException : Exception
{
    public int ErrorCode { get; }

    public McpServerException(int code, string message) : base(message)
    {
        ErrorCode = code;
    }
}
