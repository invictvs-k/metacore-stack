using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RoomServer.Models;
using RoomServer.Services;
using RoomServer.Services.ArtifactStore;

namespace RoomServer.Controllers;

public static class ArtifactEndpoints
{
  private static readonly JsonSerializerOptions SpecSerializerOptions = new(JsonSerializerDefaults.Web)
  {
    PropertyNameCaseInsensitive = true
  };

  public static IEndpointRouteBuilder MapArtifactEndpoints(this IEndpointRouteBuilder app)
  {
    app.MapPost("/rooms/{roomId}/artifacts", HandleWriteRoomArtifact);
    app.MapPost("/rooms/{roomId}/entities/{entityId}/artifacts", HandleWriteEntityArtifact);
    app.MapGet("/rooms/{roomId}/artifacts/{name}", HandleReadRoomArtifact);
    app.MapGet("/rooms/{roomId}/entities/{entityId}/artifacts/{name}", HandleReadEntityArtifact);
    app.MapGet("/rooms/{roomId}/artifacts", HandleListRoomArtifacts);
    app.MapGet("/rooms/{roomId}/entities/{entityId}/artifacts", HandleListEntityArtifacts);
    app.MapPost("/rooms/{roomId}/artifacts/promote", HandlePromoteArtifact);

    return app;
  }

  private static async Task<IResult> HandleWriteRoomArtifact(HttpContext context, string roomId, IArtifactStore store, RoomEventPublisher publisher, SessionStore sessions, PermissionService permissions, CancellationToken ct)
  {
    return await HandleWriteArtifact(context, roomId, null, "room", store, publisher, sessions, permissions, ct);
  }

  private static async Task<IResult> HandleWriteEntityArtifact(HttpContext context, string roomId, string entityId, IArtifactStore store, RoomEventPublisher publisher, SessionStore sessions, PermissionService permissions, CancellationToken ct)
  {
    return await HandleWriteArtifact(context, roomId, entityId, "entity", store, publisher, sessions, permissions, ct);
  }

  private static async Task<IResult> HandleWriteArtifact(HttpContext context, string roomId, string? entityId, string workspace, IArtifactStore store, RoomEventPublisher publisher, SessionStore sessions, PermissionService permissions, CancellationToken ct)
  {
    try
    {
      if (!TryResolveSession(context, roomId, sessions, out var session, out var errorResult))
      {
        return errorResult!;
      }

      if (!permissions.CanAccessWorkspace(session, workspace, entityId))
      {
        return ErrorFactory.HttpForbidden("PERM_DENIED", "not allowed to access workspace");
      }

      var form = await context.Request.ReadFormAsync(ct).ConfigureAwait(false);
      var specJson = form["spec"].FirstOrDefault();
      var file = form.Files["data"];
      if (specJson is null || file is null)
      {
        return Results.Json(new { error = "MissingSpecOrData", message = "spec or data not provided" }, statusCode: StatusCodes.Status400BadRequest);
      }

      ArtifactSpec? spec;
      try
      {
        spec = JsonSerializer.Deserialize<ArtifactSpec>(specJson, SpecSerializerOptions);
      }
      catch (JsonException)
      {
        return Results.Json(new { error = "InvalidSpec", message = "spec must be valid JSON" }, statusCode: StatusCodes.Status400BadRequest);
      }

      if (spec is null || string.IsNullOrWhiteSpace(spec.Name) || string.IsNullOrWhiteSpace(spec.Type))
      {
        return Results.Json(new { error = "InvalidSpec", message = "spec must include name and type" }, statusCode: StatusCodes.Status400BadRequest);
      }

      List<string>? parents = null;
      var parentsJson = form["parents"].FirstOrDefault();
      if (!string.IsNullOrWhiteSpace(parentsJson))
      {
        try
        {
          parents = JsonSerializer.Deserialize<List<string>>(parentsJson, SpecSerializerOptions);
        }
        catch (JsonException)
        {
          return Results.Json(new { error = "InvalidParents", message = "parents must be an array of strings" }, statusCode: StatusCodes.Status400BadRequest);
        }
      }

      var originEntity = workspace == "entity" ? entityId : form["entityId"].FirstOrDefault();
      originEntity ??= session.Entity.Id;

      await using var dataStream = file.OpenReadStream();
      var request = new ArtifactWriteRequest(
          roomId,
          workspace,
          originEntity,
          spec.Name!,
          spec.Type!,
          dataStream,
          file.Length,
          form["port"].FirstOrDefault(),
          parents,
          spec.Metadata);

      var manifest = await store.WriteAsync(request, ct).ConfigureAwait(false);
      await PublishArtifactEvents(publisher, roomId, manifest).ConfigureAwait(false);

      var location = workspace == "room"
          ? $"/rooms/{roomId}/artifacts/{manifest.Name}"
          : $"/rooms/{roomId}/entities/{manifest.Origin.Entity}/artifacts/{manifest.Name}";
      return Results.Created(location, new { manifest });
    }
    catch (ArtifactStoreException ex)
    {
      return Results.Json(new { error = ex.Error, message = ex.Message }, statusCode: ex.StatusCode);
    }
  }

  private static async Task<IResult> HandleReadRoomArtifact(HttpContext context, string roomId, string name, IArtifactStore store, SessionStore sessions, PermissionService permissions, CancellationToken ct)
  {
    return await HandleReadArtifact(context, roomId, null, name, "room", store, sessions, permissions, ct);
  }

  private static async Task<IResult> HandleReadEntityArtifact(HttpContext context, string roomId, string entityId, string name, IArtifactStore store, SessionStore sessions, PermissionService permissions, CancellationToken ct)
  {
    return await HandleReadArtifact(context, roomId, entityId, name, "entity", store, sessions, permissions, ct);
  }

  private static async Task<IResult> HandleReadArtifact(HttpContext context, string roomId, string? entityId, string name, string workspace, IArtifactStore store, SessionStore sessions, PermissionService permissions, CancellationToken ct)
  {
    try
    {
      if (!TryResolveSession(context, roomId, sessions, out var session, out var errorResult))
      {
        return errorResult!;
      }

      if (!permissions.CanAccessWorkspace(session, workspace, entityId))
      {
        return ErrorFactory.HttpForbidden("PERM_DENIED", "not allowed to access workspace");
      }

      var stream = await store.ReadAsync(new ArtifactReadRequest(roomId, workspace, entityId, name), ct).ConfigureAwait(false);
      var download = string.Equals(context.Request.Query["download"], "true", StringComparison.OrdinalIgnoreCase);
      var fileName = download ? name : null;
      return Results.Stream(stream, contentType: "application/octet-stream", fileDownloadName: fileName);
    }
    catch (ArtifactStoreException ex)
    {
      return Results.Json(new { error = ex.Error, message = ex.Message }, statusCode: ex.StatusCode);
    }
  }

  private static async Task<IResult> HandleListRoomArtifacts(HttpContext context, string roomId, IArtifactStore store, SessionStore sessions, PermissionService permissions, CancellationToken ct)
  {
    return await HandleListArtifacts(context, roomId, null, "room", store, sessions, permissions, ct);
  }

  private static async Task<IResult> HandleListEntityArtifacts(HttpContext context, string roomId, string entityId, IArtifactStore store, SessionStore sessions, PermissionService permissions, CancellationToken ct)
  {
    return await HandleListArtifacts(context, roomId, entityId, "entity", store, sessions, permissions, ct);
  }

  private static async Task<IResult> HandleListArtifacts(HttpContext context, string roomId, string? entityId, string workspace, IArtifactStore store, SessionStore sessions, PermissionService permissions, CancellationToken ct)
  {
    try
    {
      if (!TryResolveSession(context, roomId, sessions, out var session, out var errorResult))
      {
        return errorResult!;
      }

      if (!permissions.CanAccessWorkspace(session, workspace, entityId))
      {
        return ErrorFactory.HttpForbidden("PERM_DENIED", "not allowed to access workspace");
      }

      if (!TryParseListParameters(context.Request, out var listParams, out var listParamsError))
      {
        return listParamsError!;
      }

      var request = new ArtifactListRequest(
          roomId,
          workspace,
          entityId,
          listParams.Prefix,
          listParams.Type,
          listParams.Entity,
          listParams.Since,
          listParams.Limit,
          listParams.Offset);

      var items = await store.ListAsync(request, ct).ConfigureAwait(false);
      return Results.Ok(new { items });
    }
    catch (ArtifactStoreException ex)
    {
      return Results.Json(new { error = ex.Error, message = ex.Message }, statusCode: ex.StatusCode);
    }
  }

  private static async Task<IResult> HandlePromoteArtifact(HttpContext context, string roomId, IArtifactStore store, RoomEventPublisher publisher, SessionStore sessions, PermissionService permissions, CancellationToken ct)
  {
    try
    {
      if (!TryResolveSession(context, roomId, sessions, out var session, out var errorResult))
      {
        return errorResult!;
      }

      var payload = await context.Request.ReadFromJsonAsync<ArtifactPromotePayload>(SpecSerializerOptions, ct).ConfigureAwait(false);
      if (payload is null || string.IsNullOrWhiteSpace(payload.FromEntity) || string.IsNullOrWhiteSpace(payload.Name))
      {
        return Results.Json(new { error = "InvalidPromoteRequest", message = "fromEntity and name are required" }, statusCode: StatusCodes.Status400BadRequest);
      }

      if (!permissions.CanPromote(session, payload.FromEntity!))
      {
        return ErrorFactory.HttpForbidden("PERM_DENIED", "not allowed to promote artifact");
      }

      var manifest = await store.PromoteAsync(new ArtifactPromoteRequest(roomId, payload.FromEntity!, payload.Name!, payload.As, payload.Metadata), ct).ConfigureAwait(false);
      await PublishArtifactEvents(publisher, roomId, manifest).ConfigureAwait(false);
      var location = $"/rooms/{roomId}/artifacts/{manifest.Name}";
      return Results.Created(location, new { manifest });
    }
    catch (ArtifactStoreException ex)
    {
      return Results.Json(new { error = ex.Error, message = ex.Message }, statusCode: ex.StatusCode);
    }
    catch (JsonException)
    {
      return Results.Json(new { error = "InvalidPromoteRequest", message = "Body must be valid JSON" }, statusCode: StatusCodes.Status400BadRequest);
    }
  }

  private static bool TryResolveSession(HttpContext context, string roomId, SessionStore sessions, out EntitySession? session, out IResult? error)
  {
    session = null;
    error = null;

    var entityId = context.Request.Headers["X-Entity-Id"].FirstOrDefault();
    if (string.IsNullOrWhiteSpace(entityId))
    {
      error = ErrorFactory.HttpUnauthorized("AUTH_REQUIRED", "X-Entity-Id header is required");
      return false;
    }

    session = sessions.GetByRoomAndEntity(roomId, entityId);
    if (session is null)
    {
      error = ErrorFactory.HttpForbidden("PERM_DENIED", "active session not found for entity");
      return false;
    }

    return true;
  }

  private static async Task PublishArtifactEvents(RoomEventPublisher publisher, string roomId, ArtifactManifest manifest)
  {
    var eventType = manifest.Version == 1 ? "ARTIFACT.ADDED" : "ARTIFACT.UPDATED";
    await publisher.PublishAsync(roomId, eventType, new { manifest.Name, manifest.Version, manifest.Sha256 }).ConfigureAwait(false);
    await publisher.PublishArtifactMessageAsync(roomId, manifest).ConfigureAwait(false);
  }

  private static bool TryParseListParameters(HttpRequest request, out ArtifactListQuery parameters, out IResult? error)
  {
    parameters = new ArtifactListQuery();
    error = null;

    parameters.Prefix = request.Query["prefix"].FirstOrDefault();
    parameters.Type = request.Query["type"].FirstOrDefault();
    parameters.Entity = request.Query["entity"].FirstOrDefault();

    if (request.Query.TryGetValue("since", out var sinceValue) && !string.IsNullOrWhiteSpace(sinceValue))
    {
      if (!DateTime.TryParse(sinceValue, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
      {
        error = Results.Json(new { error = "InvalidSince", message = "since must be an ISO datetime" }, statusCode: StatusCodes.Status400BadRequest);
        return false;
      }

      parameters.Since = parsed.ToUniversalTime();
    }

    if (request.Query.TryGetValue("limit", out var limitValue) && !string.IsNullOrWhiteSpace(limitValue))
    {
      if (!int.TryParse(limitValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedLimit) || parsedLimit < 0)
      {
        error = Results.Json(new { error = "InvalidLimit", message = "limit must be a non-negative integer" }, statusCode: StatusCodes.Status400BadRequest);
        return false;
      }

      parameters.Limit = parsedLimit;
    }

    if (request.Query.TryGetValue("offset", out var offsetValue) && !string.IsNullOrWhiteSpace(offsetValue))
    {
      if (!int.TryParse(offsetValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedOffset) || parsedOffset < 0)
      {
        error = Results.Json(new { error = "InvalidOffset", message = "offset must be a non-negative integer" }, statusCode: StatusCodes.Status400BadRequest);
        return false;
      }

      parameters.Offset = parsedOffset;
    }

    return true;
  }

  private sealed class ArtifactSpec
  {
    public string? Name { get; set; }
    public string? Type { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
  }

  private sealed class ArtifactPromotePayload
  {
    public string? FromEntity { get; set; }
    public string? Name { get; set; }
    public string? As { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
  }

  private sealed class ArtifactListQuery
  {
    public string? Prefix { get; set; }
    public string? Type { get; set; }
    public string? Entity { get; set; }
    public DateTime? Since { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }
  }
}
