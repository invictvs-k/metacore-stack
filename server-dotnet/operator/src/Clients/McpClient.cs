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
}
