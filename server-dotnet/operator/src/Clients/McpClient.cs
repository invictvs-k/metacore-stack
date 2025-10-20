using RoomOperator.Abstractions;
using System.Net.Http.Json;

namespace RoomOperator.Clients;

public sealed class McpClient : IMcpClient
{
  private readonly HttpClient _httpClient;
  private readonly ILogger<McpClient> _logger;
  private readonly bool _enabled;

  public McpClient(HttpClient httpClient, ILogger<McpClient> logger, bool enabled = false)
  {
    _httpClient = httpClient;
    _logger = logger;
    _enabled = enabled;
  }

  public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
  {
    if (!_enabled)
    {
      return false;
    }

    try
    {
      var response = await _httpClient.GetAsync("/health", ct);
      return response.IsSuccessStatusCode;
    }
    catch (Exception ex)
    {
      _logger.LogWarning(ex, "MCP service is not available");
      return false;
    }
  }

  public async Task EnsureResourceAsync(string roomId, ResourceSpec spec, CancellationToken ct = default)
  {
    if (!_enabled)
    {
      _logger.LogWarning("MCP is disabled, marking resource {Name} as PENDING", spec.Name);
      return;
    }

    _logger.LogInformation("Ensuring MCP resource {Name} in room {RoomId}", spec.Name, roomId);

    var request = new
    {
      roomId,
      name = spec.Name,
      type = spec.Type,
      config = spec.Config
    };

    var response = await _httpClient.PostAsJsonAsync($"/mcp/resources", request, ct);
    response.EnsureSuccessStatusCode();
  }

  public async Task LoadMcpProvidersAsync(McpProviderConfig[] providers, CancellationToken ct = default)
  {
    if (!_enabled)
    {
      _logger.LogWarning("MCP is disabled, cannot load providers");
      return;
    }

    _logger.LogInformation("Loading {Count} MCP providers", providers.Length);

    var request = new
    {
      providers = providers.Select(p => new
      {
        id = p.Id,
        url = p.Url,
        visibility = p.Visibility
      }).ToArray()
    };

    var response = await _httpClient.PostAsJsonAsync("/admin/mcp/load", request, ct);
    response.EnsureSuccessStatusCode();

    _logger.LogInformation("MCP providers load request sent successfully");
  }

  public async Task<McpStatusResponse> GetMcpStatusAsync(CancellationToken ct = default)
  {
    if (!_enabled)
    {
      _logger.LogDebug("MCP is disabled");
      return new McpStatusResponse { Enabled = false, Providers = new List<McpProviderStatus>() };
    }

    _logger.LogDebug("Fetching MCP status");

    var response = await _httpClient.GetAsync("/admin/mcp/status", ct);
    response.EnsureSuccessStatusCode();

    var result = await response.Content.ReadFromJsonAsync<McpStatusResponse>(ct);
    return result ?? new McpStatusResponse { Enabled = true, Providers = new List<McpProviderStatus>() };
  }
}
