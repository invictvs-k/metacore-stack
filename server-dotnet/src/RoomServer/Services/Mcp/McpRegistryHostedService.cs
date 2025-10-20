using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace RoomServer.Services.Mcp;

/// <summary>
/// Hosted service that initializes the MCP Connection Manager.
/// In lazy-load mode, connections are not initiated automatically.
/// Integrates with application lifecycle for proper startup/shutdown management.
/// </summary>
public class McpRegistryHostedService : IHostedService
{
  private readonly McpConnectionManager _connectionManager;
  private readonly IConfiguration _configuration;
  private readonly ILogger<McpRegistryHostedService> _logger;
  private readonly bool _lazyLoad;

  public McpRegistryHostedService(
    McpConnectionManager connectionManager,
    IConfiguration configuration,
    ILogger<McpRegistryHostedService> logger)
  {
    _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    // Check if MCP should be loaded lazily (default: true in test mode)
    _lazyLoad = _configuration.GetValue<bool>("Mcp:LazyLoad", true);
  }

  public Task StartAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("Starting MCP Connection Manager (LazyLoad: {LazyLoad})", _lazyLoad);

    // Load provider configurations but don't connect unless explicitly disabled
    var serversConfig = _configuration.GetSection("McpServers").Get<Models.McpServerConfig[]>();

    if (serversConfig != null && serversConfig.Length > 0)
    {
      _connectionManager.LoadProviderConfigs(serversConfig);

      if (!_lazyLoad)
      {
        // Legacy behavior: connect immediately
        _logger.LogInformation("Lazy load disabled, initiating connections immediately");
        _ = Task.Run(async () =>
        {
          try
          {
            await _connectionManager.ConnectProvidersAsync();
            _logger.LogInformation("MCP providers connected");
          }
          catch (Exception ex)
          {
            _logger.LogError(ex, "Failed to connect MCP providers");
          }
        }, cancellationToken);
      }
      else
      {
        _logger.LogInformation("MCP providers loaded in idle state (connections will be initiated on-demand)");
      }
    }
    else
    {
      _logger.LogInformation("No MCP providers configured");
    }

    return Task.CompletedTask;
  }

  public Task StopAsync(CancellationToken cancellationToken)
  {
    _logger.LogInformation("Stopping MCP Connection Manager");
    _connectionManager.Dispose();
    return Task.CompletedTask;
  }
}
