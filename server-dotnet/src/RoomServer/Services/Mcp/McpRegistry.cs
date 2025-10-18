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

    public ResourceCatalog Catalog => _catalog;

    public McpRegistry(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _logger = _loggerFactory.CreateLogger<McpRegistry>();
        _catalog = new ResourceCatalog(_logger);
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

        foreach (var serverConfig in serversConfig)
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MCP server: {ServerId}", serverConfig.id);
                // Continue with other servers even if one fails
            }
        }

        _initialized = true;
        _logger.LogInformation("McpRegistry initialized with {Count} servers", _clients.Count);
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
