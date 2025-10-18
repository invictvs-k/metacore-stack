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

    public McpRegistryHostedService(McpRegistry mcpRegistry, ILogger<McpRegistryHostedService> logger)
    {
        _mcpRegistry = mcpRegistry ?? throw new ArgumentNullException(nameof(mcpRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting MCP Registry initialization");

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
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping MCP Registry");
        // Cleanup will happen when McpRegistry is disposed by DI container
        return Task.CompletedTask;
    }
}
