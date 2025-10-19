using RoomOperator.Abstractions;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace RoomOperator.Clients;

public sealed class ArtifactsClient : IArtifactsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ArtifactsClient> _logger;
    
    public ArtifactsClient(HttpClient httpClient, ILogger<ArtifactsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    
    public async Task<string?> GetArtifactHashAsync(string roomId, string name, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/room/{roomId}/artifacts/{name}/hash", ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ArtifactHashResponse>(cancellationToken: ct);
            return result?.Hash;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get artifact hash for {Name} in room {RoomId}", name, roomId);
            return null;
        }
    }
    
    public async Task SeedArtifactAsync(string roomId, ArtifactSeedSpec spec, byte[] content, CancellationToken ct = default)
    {
        _logger.LogInformation("Seeding artifact {Name} in room {RoomId}", spec.Name, roomId);
        
        using var multipartContent = new MultipartFormDataContent();
        multipartContent.Add(new StringContent(spec.Name), "name");
        multipartContent.Add(new StringContent(spec.Type), "type");
        multipartContent.Add(new StringContent(spec.Workspace), "workspace");
        multipartContent.Add(new StringContent(string.Join(",", spec.Tags ?? new List<string>())), "tags");
        multipartContent.Add(new ByteArrayContent(content), "file", spec.Name);
        
        var response = await _httpClient.PostAsync($"/room/{roomId}/artifacts/seed", multipartContent, ct);
        response.EnsureSuccessStatusCode();
    }
    
    public async Task PromoteArtifactAsync(string roomId, string name, CancellationToken ct = default)
    {
        _logger.LogInformation("Promoting artifact {Name} in room {RoomId}", name, roomId);
        
        var response = await _httpClient.PostAsync($"/room/{roomId}/artifacts/{name}/promote", null, ct);
        response.EnsureSuccessStatusCode();
    }
    
    public async Task DeleteArtifactAsync(string roomId, string name, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting artifact {Name} from room {RoomId}", name, roomId);
        
        var response = await _httpClient.DeleteAsync($"/room/{roomId}/artifacts/{name}", ct);
        
        // Ignore 404 Not Found (artifact already removed)
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogDebug("Artifact {Name} not found in room {RoomId}", name, roomId);
            return;
        }
        
        response.EnsureSuccessStatusCode();
    }
    
    public static string BuildFingerprint(ArtifactSeedSpec spec, byte[] content)
    {
        var components = $"{spec.Name}|{spec.Type}|{spec.Workspace}|{string.Join(",", spec.Tags ?? new List<string>())}|{Convert.ToBase64String(content)}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(components));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed class ArtifactHashResponse
{
    public string Hash { get; set; } = default!;
}
