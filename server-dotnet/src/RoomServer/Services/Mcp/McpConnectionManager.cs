using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RoomServer.Models;

namespace RoomServer.Services.Mcp;

/// <summary>
/// Connection states for MCP providers
/// </summary>
public enum McpState
{
    Idle,
    Connecting,
    Connected,
    Error
}

/// <summary>
/// Status information for a single MCP provider
/// </summary>
public class ProviderStatus
{
    public string Id { get; set; } = string.Empty;
    public McpState State { get; set; }
    public int Attempts { get; set; }
    public long LastChangeAt { get; set; }
    public string? LastError { get; set; }
    public long? NextRetryAt { get; set; }
}

/// <summary>
/// Manages MCP provider connections with lazy loading, state management, and rate-limited logging.
/// Connections are initiated on-demand rather than at startup.
/// </summary>
public class McpConnectionManager
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<McpConnectionManager> _logger;
    private readonly ConcurrentDictionary<string, ProviderStatus> _providerStates = new();
    private readonly ConcurrentDictionary<string, McpServerConfig> _providerConfigs = new();
    private readonly ConcurrentDictionary<string, IMcpClient> _clients = new();
    private readonly ConcurrentDictionary<string, MonitorRegistration> _monitorTasks = new();
    private readonly ConcurrentDictionary<string, object> _monitorLocks = new();
    private readonly ConcurrentDictionary<string, long> _logRateLimitGate = new();
    private readonly ResourceCatalog _catalog;
    private readonly McpDefaultsConfig? _defaults;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    
    private const int MaxRetryAttempts = 5;
    private const int MaxBackoffSeconds = 60;
    private const int LogRateLimitWindowMs = 60000; // 1 minute
    private const int MonitorRegistrationRetryLimit = 5;
    private static readonly TimeSpan MonitorRegistrationRetryDelay = TimeSpan.FromMilliseconds(50);
    
    private readonly CancellationTokenSource _shutdownCts = new();
    private int _disposed = 0;

    public ResourceCatalog Catalog => _catalog;

    private sealed class MonitorRegistration
    {
        public MonitorRegistration(Task task, Guid token)
        {
            Task = task;
            Token = token;
        }

        public Task Task { get; }

        public Guid Token { get; }
    }

    public McpConnectionManager(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = _loggerFactory.CreateLogger<McpConnectionManager>();
        _catalog = new ResourceCatalog(_loggerFactory.CreateLogger<ResourceCatalog>());
        
        // Read defaults from configuration
        _defaults = configuration.GetSection("McpDefaults").Get<McpDefaultsConfig>();
    }

    /// <summary>
    /// Loads provider configurations without initiating connections.
    /// Providers remain in Idle state until ConnectProvidersAsync is called.
    /// </summary>
    public void LoadProviderConfigs(McpServerConfig[] configs)
    {
        if (configs == null || configs.Length == 0)
        {
            _logger.LogInformation("No MCP provider configs to load");
            return;
        }

        foreach (var config in configs)
        {
            _providerConfigs[config.id] = config;
            SetState(config.id, McpState.Idle);
            _logger.LogInformation("Loaded MCP provider config: {ProviderId}", config.id);
        }
    }

    /// <summary>
    /// Initiates connection to all configured providers.
    /// </summary>
    public async Task ConnectProvidersAsync()
    {
        if (_providerConfigs.IsEmpty)
        {
            _logger.LogWarning("No MCP providers configured for connection");
            return;
        }

        _logger.LogInformation("Initiating connection to {Count} MCP providers", _providerConfigs.Count);
        
        var tasks = _providerConfigs.Keys.Select(id => ConnectProviderAsync(id)).ToArray();
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Connects a single provider with retry logic and exponential backoff.
    /// </summary>
    private async Task ConnectProviderAsync(string providerId)
    {
        if (!_providerConfigs.TryGetValue(providerId, out var config))
        {
            _logger.LogWarning("Provider config not found: {ProviderId}", providerId);
            return;
        }

        await ConnectProviderAsync(providerId, config);
    }

    private async Task ConnectProviderAsync(string providerId, McpServerConfig config)
    {
        await _connectionLock.WaitAsync();
        try
        {
            // Create client if it doesn't exist
            if (!_clients.TryGetValue(providerId, out var client))
            {
                client = new McpClient(
                    config.id,
                    config.url,
                    _loggerFactory.CreateLogger<McpClient>());
                _clients[providerId] = client;
            }

            SetState(providerId, McpState.Connecting);
            
            var status = _providerStates[providerId];
            int attempts = 0;
            
            while (attempts < MaxRetryAttempts && !_disposed)
            {
                attempts++;
                status.Attempts = attempts;
                
                try
                {
                    await client.ConnectAsync();
                    
                    if (client.IsConnected)
                    {
                        SetState(providerId, McpState.Connected);
                        await LoadAndRegisterToolsAsync(providerId, client, config);

                        // Start monitoring for reconnection in background
                        await EnsureConnectionMonitorAsync(providerId, client, config);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    if (ShouldLogError(providerId))
                    {
                        _logger.LogWarning(ex, 
                            "[{ProviderId}] Connection attempt {Attempt}/{Max} failed: {Error}", 
                            providerId, attempts, MaxRetryAttempts, ex.Message);
                    }
                }

                if (attempts < MaxRetryAttempts)
                {
                    var backoffMs = CalculateBackoff(attempts);
                    status.NextRetryAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + backoffMs;
                    await Task.Delay(backoffMs);
                }
            }

            SetState(providerId, McpState.Error, "Max connection attempts reached");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    /// <summary>
    /// Monitors connection and attempts reconnection if disconnected.
    /// </summary>
    private async Task MonitorConnectionAsync(string providerId, IMcpClient client, McpServerConfig config, Guid registrationToken, CancellationToken shutdownToken)
    {
        try
        {
            while (!_disposed && !shutdownToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(5000, shutdownToken);

                    if (!client.IsConnected && !_disposed && !shutdownToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("[{ProviderId}] Connection lost, attempting to reconnect", providerId);
                        SetState(providerId, McpState.Connecting);
                        await ConnectProviderAsync(providerId, config);

                        if (client.IsConnected)
                        {
                            SetState(providerId, McpState.Connected);
                        }
                    }
                }
                catch (OperationCanceledException) when (shutdownToken.IsCancellationRequested)
                {
                    // Shutdown was requested, exit gracefully.
                    break;
                }
                catch (Exception ex)
                {
                    if (ShouldLogError(providerId))
                    {
                        _logger.LogError(ex, "[{ProviderId}] Error in connection monitor", providerId);
                    }
                    try
                    {
                        await Task.Delay(10000, shutdownToken);
                    }
                    catch (OperationCanceledException) when (shutdownToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
        }
        finally
        {
            var monitorLock = GetMonitorLock(providerId);

            lock (monitorLock)
            {
                if (_monitorTasks.TryGetValue(providerId, out var registration) && registration.Token == registrationToken)
                {
                    _monitorTasks.TryRemove(providerId, out _);
                    if (_monitorLocks.TryGetValue(providerId, out var currentLock) && ReferenceEquals(currentLock, monitorLock))
                    {
                        _monitorLocks.TryRemove(providerId, out _);
                    }
                }
            }
        }
    }

    private async Task EnsureConnectionMonitorAsync(string providerId, IMcpClient client, McpServerConfig config)
    {
        if (_shutdownCts.IsCancellationRequested)
        {
            return;
        }

        var monitorLock = GetMonitorLock(providerId);

        for (var attempts = 0; attempts < MonitorRegistrationRetryLimit; attempts++)
        {
            MonitorRegistration registration;

            lock (monitorLock)
            {
                if (_monitorTasks.TryGetValue(providerId, out var existing) && !existing.Task.IsCompleted)
                {
                    return;
                }

                registration = StartMonitorTask(providerId, client, config);
                _monitorTasks[providerId] = registration;
            }

            if (!registration.Task.IsCompleted)
            {
                return;
            }

            if (!registration.Task.IsFaulted && !registration.Task.IsCanceled)
            {
                if (!_disposed && !_shutdownCts.IsCancellationRequested)
                {
                    _logger.LogWarning("[{ProviderId}] Monitor task for provider exited unexpectedly without error", providerId);
                }

                // A successfully completed monitor indicates the loop exited gracefully (e.g. during shutdown),
                // so avoid respawning another watcher and just return.
                return;
            }

            if (attempts + 1 >= MonitorRegistrationRetryLimit)
            {
                _logger.LogWarning("[{ProviderId}] Connection monitor restart attempts exceeded retry limit", providerId);
                return;
            }

            await Task.Delay(MonitorRegistrationRetryDelay);
        }
    }

    private MonitorRegistration StartMonitorTask(string providerId, IMcpClient client, McpServerConfig config)
    {
        var token = Guid.NewGuid();
        var shutdownToken = _shutdownCts.Token;
        var monitorTask = Task.Factory.StartNew(
                () => MonitorConnectionAsync(providerId, client, config, token, shutdownToken),
                shutdownToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default)
            .Unwrap();
        return new MonitorRegistration(monitorTask, token);
    }

    private object GetMonitorLock(string providerId)
    {
        return _monitorLocks.GetOrAdd(providerId, _ => new object());
    }

    /// <summary>
    /// Loads tools from a connected provider and registers them in the catalog.
    /// </summary>
    private async Task LoadAndRegisterToolsAsync(string providerId, IMcpClient client, McpServerConfig config)
    {
        try
        {
            var tools = await client.ListToolsAsync();
            _logger.LogInformation("[{ProviderId}] Loaded {Count} tools", providerId, tools.Length);

            foreach (var tool in tools)
            {
                var mergedPolicy = MergePolicies(tool.policy, _defaults, config.visibility);
                var mergedSpec = tool with { policy = mergedPolicy };
                _catalog.Register(providerId, mergedSpec, client);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{ProviderId}] Failed to load and register tools", providerId);
        }
    }

    /// <summary>
    /// Gets the current status of all providers.
    /// </summary>
    public ProviderStatus[] GetStatus()
    {
        return _providerStates.Values.ToArray();
    }

    /// <summary>
    /// Gets a specific client by provider ID.
    /// </summary>
    public IMcpClient? GetClient(string providerId)
    {
        _clients.TryGetValue(providerId, out var client);
        return client;
    }

    /// <summary>
    /// Updates provider state and logs transition if state changed.
    /// </summary>
    private void SetState(string providerId, McpState newState, string? error = null)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var prev = _providerStates.GetValueOrDefault(providerId);
        
        var status = new ProviderStatus
        {
            Id = providerId,
            State = newState,
            Attempts = prev?.Attempts ?? 0,
            LastChangeAt = now,
            LastError = error ?? (newState == McpState.Error ? prev?.LastError : null),
            NextRetryAt = prev?.NextRetryAt
        };

        // Log only on state transition
        if (prev == null || prev.State != newState)
        {
            _logger.LogInformation(
                "[{ProviderId}] State transition: {FromState} -> {ToState}", 
                providerId, 
                prev?.State.ToString() ?? "None", 
                newState);
        }

        _providerStates[providerId] = status;
    }

    /// <summary>
    /// Determines if an error should be logged based on rate limiting.
    /// </summary>
    private bool ShouldLogError(string providerId)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var lastLog = _logRateLimitGate.GetValueOrDefault(providerId, 0);
        
        if (now - lastLog > LogRateLimitWindowMs)
        {
            _logRateLimitGate[providerId] = now;
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Calculates exponential backoff with jitter.
    /// </summary>
    private static int CalculateBackoff(int attempt)
    {
        var baseDelay = Math.Min(500 * Math.Pow(2, attempt - 1), MaxBackoffSeconds * 1000);
        var jitter = Random.Shared.Next(0, (int)baseDelay);
        return (int)baseDelay + jitter;
    }

    /// <summary>
    /// Merges tool policy with defaults and server configuration.
    /// </summary>
    private static ToolPolicy MergePolicies(ToolPolicy? toolPolicy, McpDefaultsConfig? defaults, string? serverVisibility)
    {
        return new ToolPolicy(
            visibility: toolPolicy?.visibility ?? serverVisibility ?? "room",
            allowedEntities: toolPolicy?.allowedEntities ?? defaults?.allowedEntities ?? "public",
            scopes: toolPolicy?.scopes ?? defaults?.scopes ?? Array.Empty<string>(),
            rateLimit: toolPolicy?.rateLimit ?? defaults?.rateLimit ?? new RateLimit(60)
        );
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _shutdownCts.Cancel();

        foreach (var client in _clients.Values)
        {
            if (client is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        _clients.Clear();
        var monitorTasks = _monitorTasks.Values.Select(registration => registration.Task).ToArray();
        if (monitorTasks.Length > 0)
        {
            try
            {
                Task.WaitAll(monitorTasks, TimeSpan.FromSeconds(5));
            }
            catch (AggregateException ex)
            {
                foreach (var inner in ex.InnerExceptions)
                {
                    if (inner is not OperationCanceledException && inner is not TaskCanceledException)
                    {
                        _logger.LogDebug(inner, "Error while waiting for monitor tasks to shut down");
                    }
                }
            }
        }

        _monitorTasks.Clear();
        _monitorLocks.Clear();
        _shutdownCts.Dispose();
        _connectionLock.Dispose();
    }
}
