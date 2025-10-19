using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RoomServer.Models;

namespace RoomServer.Services.Mcp;

/// <summary>
/// Manages the lifecycle of multiple MCP client connections.
/// </summary>
public class McpRegistry : IDisposable
{
  private readonly IConfiguration _configuration;
  private readonly ILoggerFactory _loggerFactory;
  private readonly ILogger<McpRegistry> _logger;
  private readonly ConcurrentDictionary<string, IMcpClient> _clients = new();
  private readonly ResourceCatalog _catalog;
  private bool _initialized = false;
  private bool _disposed = false;

  public ResourceCatalog Catalog => _catalog;

  public McpRegistry(IConfiguration configuration, ILoggerFactory loggerFactory)
  {
    _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    _logger = _loggerFactory.CreateLogger<McpRegistry>();
    _catalog = new ResourceCatalog(_loggerFactory.CreateLogger<ResourceCatalog>());
  }

  /// <summary>
  /// Initializes all configured MCP server connections.
  /// </summary>
  public async Task InitializeAsync()
  {
    if (_initialized)
    {
      _logger.LogWarning("McpRegistry already initialized");
      return;
    }

    var serversConfig = _configuration.GetSection("McpServers").Get<McpServerConfig[]>();
    if (serversConfig == null || serversConfig.Length == 0)
    {
      _logger.LogWarning("No MCP servers configured");
      _initialized = true;
      return;
    }

    var defaultsConfig = _configuration.GetSection("McpDefaults").Get<McpDefaultsConfig>();

    // Initialize servers concurrently to avoid sequential delays
    var initTasks = serversConfig.Select(serverConfig => InitializeServerAsync(serverConfig, defaultsConfig)).ToArray();
    await Task.WhenAll(initTasks);

    _initialized = true;
    _logger.LogInformation("McpRegistry initialized with {Count} servers", _clients.Count);
  }

  /// <summary>
  /// Initializes a single MCP server with automatic retry on reconnection.
  /// </summary>
  private async Task InitializeServerAsync(McpServerConfig serverConfig, McpDefaultsConfig? defaultsConfig)
  {
    try
    {
      _logger.LogInformation("Initializing MCP server: {ServerId} at {Url}", serverConfig.id, serverConfig.url);

      var client = new McpClient(
          serverConfig.id,
          serverConfig.url,
          _loggerFactory.CreateLogger<McpClient>());

      _clients[serverConfig.id] = client;

      // Connect and load tools
      await client.ConnectAsync();
      await LoadAndRegisterToolsAsync(client, serverConfig, defaultsConfig);

      // Start background task to re-register tools on reconnection
      _ = Task.Run(async () => await MonitorAndReregisterToolsAsync(client, serverConfig, defaultsConfig));
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to initialize MCP server: {ServerId}", serverConfig.id);

      // Even if initial connection fails, start monitoring for future reconnections
      if (_clients.TryGetValue(serverConfig.id, out var client))
      {
        _ = Task.Run(async () => await MonitorAndReregisterToolsAsync(client, serverConfig, defaultsConfig));
      }
    }
  }

  /// <summary>
  /// Monitors client connection and re-registers tools when reconnected.
  /// </summary>
  private async Task MonitorAndReregisterToolsAsync(IMcpClient client, McpServerConfig serverConfig, McpDefaultsConfig? defaultsConfig)
  {
    while (!_disposed)
    {
      try
      {
        // Wait for client to be connected
        while (!client.IsConnected && !_disposed)
        {
          await Task.Delay(5000); // Check every 5 seconds
        }

        if (_disposed) break;

        // Load and register tools
        await LoadAndRegisterToolsAsync(client, serverConfig, defaultsConfig);

        // Wait for disconnection before checking again
        while (client.IsConnected && !_disposed)
        {
          await Task.Delay(5000);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error monitoring MCP server: {ServerId}", serverConfig.id);
        await Task.Delay(10000); // Wait before retrying
      }
    }
  }

  /// <summary>
  /// Loads tools from client and registers them in the catalog.
  /// </summary>
  private async Task LoadAndRegisterToolsAsync(IMcpClient client, McpServerConfig serverConfig, McpDefaultsConfig? defaultsConfig)
  {
    var tools = await client.ListToolsAsync();
    _logger.LogInformation("Loaded {Count} tools from {ServerId}", tools.Length, serverConfig.id);

    // Register tools in catalog with merged policies
    foreach (var tool in tools)
    {
      var mergedPolicy = MergePolicies(tool.policy, defaultsConfig, serverConfig.visibility);
      var mergedSpec = tool with { policy = mergedPolicy };

      _catalog.Register(serverConfig.id, mergedSpec, client);
    }
  }

  /// <summary>
  /// Gets a client by server ID.
  /// </summary>
  public IMcpClient? GetClient(string serverId)
  {
    _clients.TryGetValue(serverId, out var client);
    return client;
  }

  /// <summary>
  /// Merges tool-specific policy with defaults and server configuration.
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
    _disposed = true;

    foreach (var client in _clients.Values)
    {
      if (client is IDisposable disposable)
      {
        disposable.Dispose();
      }
    }
    _clients.Clear();
  }
}
