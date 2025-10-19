using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RoomServer.Services.Mcp;

/// <summary>
/// Hosted service that initializes the MCP Registry in the background.
/// Integrates with application lifecycle for proper startup/shutdown management.
/// </summary>
public class McpRegistryHostedService : IHostedService
{
    private readonly McpRegistry _mcpRegistry;
    private readonly ILogger<McpRegistryHostedService> _logger;
    private Task? _initializationTask;
    private CancellationTokenSource? _cancellationTokenSource;

    public McpRegistryHostedService(McpRegistry mcpRegistry, ILogger<McpRegistryHostedService> logger)
    {
        _mcpRegistry = mcpRegistry ?? throw new ArgumentNullException(nameof(mcpRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting MCP Registry initialization in background");

        // Create our own CTS to control the initialization lifecycle
        _cancellationTokenSource = new CancellationTokenSource();

        // Start initialization in background without blocking application startup
        _initializationTask = Task.Run(async () =>
        {
            try
            {
                await _mcpRegistry.InitializeAsync();
                _logger.LogInformation("MCP Registry initialization completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MCP Registry");
                // Don't rethrow - we want the application to start even if MCP initialization fails
            }
        }, _cancellationTokenSource.Token);

        // Return immediately to avoid blocking application startup
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping MCP Registry");

        // Cancel any ongoing initialization
        try
        {
            _cancellationTokenSource?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Token source may already be disposed
        }

        // Wait for initialization task to complete (with timeout)
        if (_initializationTask != null)
        {
            try
            {
                await Task.WhenAny(_initializationTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));
            }
            catch (OperationCanceledException)
            {
                // Expected when application is shutting down
            }
        }

        _cancellationTokenSource?.Dispose();
    }
}
