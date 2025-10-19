using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Mvc.Testing;
using RoomServer.Models;
using Xunit;

namespace RoomServer.Tests;

public class SecurityTests : IAsyncLifetime
{
  private readonly WebApplicationFactory<Program> _factory = new();
  private const string RoomId = "room-security01";

  [Fact]
  public async Task JoinOwnerWithoutAuthShouldFail()
  {
    await using var connection = BuildConnection();
    await connection.StartAsync();

    var exception = await Assert.ThrowsAsync<HubException>(() => connection.InvokeAsync<IReadOnlyCollection<EntitySpec>>("Join", RoomId, new EntitySpec
    {
      Id = "E-OWNER",
      Kind = "human",
      Visibility = "owner",
      OwnerUserId = "U-1"
    }));

    var error = JsonDocument.Parse(exception.Message);
    error.RootElement.GetProperty("code").GetString().Should().Be("AUTH_REQUIRED");
  }

  [Fact]
  public async Task JoinOwnerWithAuthSucceeds()
  {
    await using var connection = BuildConnection(options => options.Headers.Add("X-User-Id", "U-1"));
    await connection.StartAsync();

    var entities = await connection.InvokeAsync<IReadOnlyCollection<EntitySpec>>("Join", RoomId, new EntitySpec
    {
      Id = "E-OWNER",
      Kind = "human",
      Visibility = "owner",
      OwnerUserId = "U-1"
    });

    entities.Should().Contain(e => e.Id == "E-OWNER");
  }

  [Fact]
  public async Task CommandDeniedWhenPolicyRequiresOrchestrator()
  {
    await using var humanConnection = BuildConnection();
    await using var targetConnection = BuildConnection();

    await humanConnection.StartAsync();
    await targetConnection.StartAsync();

    await humanConnection.InvokeAsync("Join", RoomId, new EntitySpec
    {
      Id = "E-Human",
      Kind = "human"
    });

    await targetConnection.InvokeAsync("Join", RoomId, new EntitySpec
    {
      Id = "E-Target",
      Kind = "agent",
      Policy = new PolicySpec { AllowCommandsFrom = "orchestrator" }
    });

    var payload = JsonDocument.Parse("{\"target\":\"E-T\"}").RootElement.Clone();

    var exception = await Assert.ThrowsAsync<HubException>(() => humanConnection.InvokeAsync("SendToRoom", RoomId, new MessageModel
    {
      From = "E-Human",
      Channel = "room",
      Type = "command",
      Payload = payload
    }));

    var error = JsonDocument.Parse(exception.Message);
    error.RootElement.GetProperty("code").GetString().Should().Be("PERM_DENIED");
  }

  [Fact]
  public async Task DirectMessageToOwnerFromDifferentUserIsDenied()
  {
    await using var ownerConnection = BuildConnection(options => options.Headers.Add("X-User-Id", "U-2"));
    await using var userConnection = BuildConnection(options => options.Headers.Add("X-User-Id", "U-1"));

    await ownerConnection.StartAsync();
    await userConnection.StartAsync();

    await ownerConnection.InvokeAsync("Join", RoomId, new EntitySpec
    {
      Id = "E-TARGET",
      Kind = "agent",
      Visibility = "owner",
      OwnerUserId = "U-2"
    });

    await userConnection.InvokeAsync("Join", RoomId, new EntitySpec
    {
      Id = "E-USER",
      Kind = "human",
      OwnerUserId = "U-1"
    });

    var exception = await Assert.ThrowsAsync<HubException>(() => userConnection.InvokeAsync("SendToRoom", RoomId, new MessageModel
    {
      From = "E-USER",
      Channel = "@E-TARGET",
      Type = "chat",
      Payload = new { text = "ping" }
    }));

    var error = JsonDocument.Parse(exception.Message);
    error.RootElement.GetProperty("code").GetString().Should().Be("PERM_DENIED");
  }

  [Fact]
  public async Task PrivateWorkspaceWriteByDifferentEntityIsDenied()
  {
    await using var connection = BuildConnection();
    await connection.StartAsync();

    await connection.InvokeAsync("Join", RoomId, new EntitySpec
    {
      Id = "E-Alice",
      Kind = "human"
    });

    using var content = new MultipartFormDataContent();
    content.Add(new StringContent("{\"name\":\"test.txt\",\"type\":\"text/plain\"}", Encoding.UTF8, "application/json"), "spec");
    content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("hello")) { Headers = { ContentType = new MediaTypeHeaderValue("text/plain") } }, "data", "test.txt");

    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Entity-Id", "E-Alice");
    var response = await client.PostAsync($"/rooms/{RoomId}/entities/E-B/artifacts", content);

    response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    body.RootElement.GetProperty("code").GetString().Should().Be("PERM_DENIED");
  }

  [Fact]
  public async Task PromoteDeniedForNonOwner()
  {
    await using var ownerConnection = BuildConnection();
    await ownerConnection.StartAsync();

    await ownerConnection.InvokeAsync("Join", RoomId, new EntitySpec
    {
      Id = "E-Alice",
      Kind = "human"
    });

    using var content = new MultipartFormDataContent();
    content.Add(new StringContent("{\"name\":\"test.txt\",\"type\":\"text/plain\"}", Encoding.UTF8, "application/json"), "spec");
    content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("hello")) { Headers = { ContentType = new MediaTypeHeaderValue("text/plain") } }, "data", "test.txt");

    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("X-Entity-Id", "E-Alice");
    var uploadResponse = await client.PostAsync($"/rooms/{RoomId}/entities/E-A/artifacts", content);
    uploadResponse.EnsureSuccessStatusCode();

    client.DefaultRequestHeaders.Remove("X-Entity-Id");
    client.DefaultRequestHeaders.Add("X-Entity-Id", "E-Bob");
    var promotePayload = JsonContent.Create(new { fromEntity = "E-Alice", name = "test.txt" });
    var promoteResponse = await client.PostAsync($"/rooms/{RoomId}/artifacts/promote", promotePayload);

    promoteResponse.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden);
    var body = JsonDocument.Parse(await promoteResponse.Content.ReadAsStringAsync());
    body.RootElement.GetProperty("code").GetString().Should().Be("PERM_DENIED");
  }

  private HubConnection BuildConnection(Action<Microsoft.AspNetCore.Http.Connections.Client.HttpConnectionOptions>? configure = null)
  {
    return new HubConnectionBuilder()
        .WithUrl("http://localhost/room", options =>
        {
          options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
          options.Transports = HttpTransportType.LongPolling;
          configure?.Invoke(options);
        })
        .WithAutomaticReconnect()
        .Build();
  }

  public Task InitializeAsync() => Task.CompletedTask;

  public Task DisposeAsync() => Task.CompletedTask;
}
