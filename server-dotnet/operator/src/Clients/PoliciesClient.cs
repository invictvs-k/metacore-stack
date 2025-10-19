using RoomOperator.Abstractions;
using System.Net.Http.Json;

namespace RoomOperator.Clients;

public sealed class PoliciesClient : IPoliciesClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PoliciesClient> _logger;
    
    public PoliciesClient(HttpClient httpClient, ILogger<PoliciesClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task ApplyPolicyAsync(string roomId, string policyName, object policyValue, CancellationToken ct = default)
    {
        _logger.LogInformation("Applying policy {PolicyName} to room {RoomId}", policyName, roomId);
        
        var request = new
        {
            roomId,
            policyName,
            policyValue
        };
        
        var response = await _httpClient.PostAsJsonAsync($"/room/{roomId}/policies", request, ct);
        response.EnsureSuccessStatusCode();
    }
}
