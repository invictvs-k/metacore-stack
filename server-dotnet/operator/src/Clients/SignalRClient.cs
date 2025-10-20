using Microsoft.AspNetCore.SignalR.Client;
using RoomOperator.Abstractions;
using System.Net.Http.Json;

namespace RoomOperator.Clients;

public sealed class SignalRClient : IRoomClient, IAsyncDisposable
{
  private readonly HttpClient _httpClient;
  private readonly HubConnection? _hubConnection;
  private readonly ILogger<SignalRClient> _logger;

  public SignalRClient(HttpClient httpClient, ILogger<SignalRClient> logger, string? hubUrl = null)
  {
    _httpClient = httpClient;
    _logger = logger;

    if (!string.IsNullOrEmpty(hubUrl))
    {
      _hubConnection = new HubConnectionBuilder()
          .WithUrl(hubUrl)
          .WithAutomaticReconnect()
          .Build();
    }
  }

  public async Task<RoomState> GetStateAsync(string roomId, CancellationToken ct = default)
  {
    _logger.LogInformation("Fetching room state for {RoomId}", roomId);

    var response = await _httpClient.GetAsync($"/room/{roomId}/state", ct);
    response.EnsureSuccessStatusCode();

    var state = await response.Content.ReadFromJsonAsync<RoomState>(cancellationToken: ct);
    return state ?? new RoomState { RoomId = roomId };
  }

  public async Task JoinEntityAsync(string roomId, EntitySpec entity, CancellationToken ct = default)
  {
    _logger.LogInformation("Joining entity {EntityId} to room {RoomId}", entity.Id, roomId);

    var request = new
    {
      roomId,
      entityId = entity.Id,
      kind = entity.Kind,
      displayName = entity.DisplayName,
      visibility = entity.Visibility,
      ownerUserId = entity.OwnerUserId,
      capabilities = entity.Capabilities,
      policy = new
      {
        allow_commands_from = entity.Policy.AllowCommandsFrom,
        sandbox_mode = entity.Policy.SandboxMode,
        env_whitelist = entity.Policy.EnvWhitelist,
        scopes = entity.Policy.Scopes
      }
    };

    var response = await _httpClient.PostAsJsonAsync($"/room/{roomId}/join", request, ct);

    // Ignore 409 Conflict (entity already exists with same spec)
    if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
    {
      _logger.LogDebug("Entity {EntityId} already exists in room {RoomId}", entity.Id, roomId);
      return;
    }

    response.EnsureSuccessStatusCode();
  }

  public async Task KickEntityAsync(string roomId, string entityId, CancellationToken ct = default)
  {
    _logger.LogInformation("Kicking entity {EntityId} from room {RoomId}", entityId, roomId);

    var response = await _httpClient.DeleteAsync($"/room/{roomId}/entities/{entityId}", ct);

    // Ignore 404 Not Found (entity already removed)
    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
      _logger.LogDebug("Entity {EntityId} not found in room {RoomId}", entityId, roomId);
      return;
    }

    response.EnsureSuccessStatusCode();
  }

  public async ValueTask DisposeAsync()
  {
    if (_hubConnection != null)
    {
      await _hubConnection.DisposeAsync();
    }
  }
}
