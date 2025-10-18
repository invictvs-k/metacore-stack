using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using NUlid;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using RoomServer.Models;
using RoomServer.Services;
using RoomServer.Services.Mcp;

namespace RoomServer.Hubs;

public partial class RoomHub : Hub
{
    private readonly SessionStore _sessions;
    private readonly PermissionService _permissions;
    private readonly RoomEventPublisher _events;
    private readonly ILogger<RoomHub> _logger;
    private readonly McpRegistry _mcpRegistry;
    private readonly PolicyEngine _policyEngine;

    public RoomHub(
        SessionStore sessions, 
        PermissionService permissions, 
        RoomEventPublisher events, 
        ILogger<RoomHub> logger,
        McpRegistry mcpRegistry,
        PolicyEngine policyEngine)
    {
        _sessions = sessions;
        _permissions = permissions;
        _events = events;
        _logger = logger;
        _mcpRegistry = mcpRegistry;
        _policyEngine = policyEngine;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var removed = _sessions.RemoveByConnection(Context.ConnectionId);
        if (removed is not null)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, removed.RoomId);
            await _events.PublishAsync(removed.RoomId, "ENTITY.LEAVE", new { entityId = removed.Entity.Id });
            await PublishRoomState(removed.RoomId);
            _logger.LogInformation("[{RoomId}] {EntityId} disconnected ({Kind})", removed.RoomId, removed.Entity.Id, removed.Entity.Kind);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task<IReadOnlyCollection<EntitySpec>> Join(string roomId, EntitySpec entity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);
        ArgumentNullException.ThrowIfNull(entity);
        ValidateEntity(entity);

        var userId = ResolveUserId();
        if (string.Equals(entity.Visibility, "owner", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(entity.OwnerUserId))
            {
                throw ErrorFactory.HubBadRequest("INVALID_ENTITY_SPEC", "owner visibility requires owner_user_id");
            }

            if (string.IsNullOrWhiteSpace(userId))
            {
                throw ErrorFactory.HubForbidden("AUTH_REQUIRED", "owner visibility requires authentication");
            }

            if (!string.Equals(entity.OwnerUserId, userId, StringComparison.Ordinal))
            {
                throw ErrorFactory.HubForbidden("PERM_DENIED", "owner mismatch");
            }
        }

        _sessions.RemoveByConnection(Context.ConnectionId);

        var normalized = NormalizeEntity(entity);
        var session = new EntitySession
        {
            ConnectionId = Context.ConnectionId,
            RoomId = roomId,
            Entity = normalized,
            JoinedAt = DateTime.UtcNow,
            UserId = userId
        };

        _sessions.Add(session);

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await _events.PublishAsync(roomId, "ENTITY.JOIN", new { entity = normalized });
        await PublishRoomState(roomId);

        _logger.LogInformation("[{RoomId}] {EntityId} joined ({Kind})", roomId, normalized.Id, normalized.Kind);

        return _sessions.ListByRoom(roomId).Select(s => s.Entity).ToList();
    }

    public async Task Leave(string roomId, string entityId)
    {
        var session = _sessions.GetByConnection(Context.ConnectionId) ?? throw ErrorFactory.HubUnauthorized("AUTH_REQUIRED", "session not found");

        if (!string.Equals(session.RoomId, roomId, StringComparison.Ordinal) ||
            !string.Equals(session.Entity.Id, entityId, StringComparison.Ordinal))
        {
            throw ErrorFactory.HubForbidden("PERM_DENIED", "cannot leave on behalf of another entity");
        }

        _sessions.RemoveByConnection(Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);

        await _events.PublishAsync(roomId, "ENTITY.LEAVE", new { entityId = session.Entity.Id });
        await PublishRoomState(roomId);

        _logger.LogInformation("[{RoomId}] {EntityId} left ({Kind})", roomId, session.Entity.Id, session.Entity.Kind);
    }

    public async Task SendToRoom(string roomId, MessageModel message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);
        ArgumentNullException.ThrowIfNull(message);

        var fromSession = _sessions.GetByConnection(Context.ConnectionId) ?? throw ErrorFactory.HubUnauthorized("AUTH_REQUIRED", "session not found");
        if (!string.Equals(fromSession.RoomId, roomId, StringComparison.Ordinal))
        {
            throw ErrorFactory.HubForbidden("PERM_DENIED", "cannot publish to different room");
        }

        if (!string.Equals(message.From, fromSession.Entity.Id, StringComparison.Ordinal))
        {
            throw ErrorFactory.HubForbidden("PERM_DENIED", "message sender mismatch");
        }

        message.RoomId = roomId;
        message.Ts = DateTime.UtcNow;
        message.Id = string.IsNullOrWhiteSpace(message.Id) ? Ulid.NewUlid().ToString() : message.Id;

        if (IsDirectMessage(message.Channel))
        {
            await HandleDirectMessage(roomId, message, fromSession);
        }
        else
        {
            await HandleRoomMessage(roomId, message, fromSession);
        }

        _logger.LogInformation("[{RoomId}] {From} → {Channel} :: {Type}", roomId, message.From, message.Channel, message.Type);
    }

    public Task<IReadOnlyCollection<EntitySpec>> ListEntities(string roomId)
    {
        var entities = _sessions.ListByRoom(roomId).Select(s => s.Entity).ToList();
        return Task.FromResult<IReadOnlyCollection<EntitySpec>>(entities);
    }

    private async Task HandleDirectMessage(string roomId, MessageModel message, EntitySession fromSession)
    {
        var targetId = message.Channel[1..];
        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw ErrorFactory.HubBadRequest("INVALID_TARGET", "direct messages require a target");
        }

        var targetSession = _sessions.GetByRoomAndEntity(roomId, targetId);
        if (targetSession is null)
        {
            throw ErrorFactory.HubNotFound("TARGET_NOT_FOUND", "target not connected");
        }

        if (!_permissions.CanDirectMessage(fromSession, targetSession.Entity))
        {
            throw ErrorFactory.HubForbidden("PERM_DENIED", "not allowed to direct message target");
        }

        await Clients.Clients(new[] { fromSession.ConnectionId, targetSession.ConnectionId }).SendAsync("message", message);
    }

    private async Task HandleRoomMessage(string roomId, MessageModel message, EntitySession fromSession)
    {
        if (string.Equals(message.Type, "command", StringComparison.OrdinalIgnoreCase))
        {
            var targetId = ResolveCommandTarget(message.Payload);
            if (string.IsNullOrWhiteSpace(targetId))
            {
                throw ErrorFactory.HubBadRequest("INVALID_COMMAND", "command payload must include target");
            }

            var target = _sessions.GetByRoomAndEntity(roomId, targetId)?.Entity;
            if (target is null)
            {
                throw ErrorFactory.HubNotFound("TARGET_NOT_FOUND", "command target not connected");
            }

            if (!_permissions.CanSendCommand(fromSession, target))
            {
                throw ErrorFactory.HubForbidden("PERM_DENIED", "not allowed to command this target");
            }
        }

        await Clients.Group(roomId).SendAsync("message", message);
    }

    /// <summary>
    /// Resolves the target entity ID from a command payload.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method handles multiple payload types that can be received from different serialization contexts:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="JsonElement"/> - When payload is deserialized by System.Text.Json inline</description></item>
    /// <item><description><see cref="JsonDocument"/> - When payload is pre-parsed as a JSON document</description></item>
    /// <item><description><see cref="IDictionary{TKey, TValue}"/> of string to object - When payload is a strongly-typed dictionary</description></item>
    /// <item><description><see cref="IDictionary{TKey, TValue}"/> of string to string - When payload contains only string values</description></item>
    /// </list>
    /// <para>
    /// The method attempts to extract the "target" property from the payload, which identifies the entity 
    /// that should receive the command. The property must be a string value representing an entity ID.
    /// </para>
    /// </remarks>
    /// <param name="payload">The command payload object, which should contain a "target" property.</param>
    /// <returns>
    /// The target entity ID as a string if found; otherwise, <c>null</c> if the payload type is not recognized 
    /// or if the "target" property is not present or cannot be converted to a string.
    /// </returns>
    private static string? ResolveCommandTarget(object payload)
    {
        switch (payload)
        {
            case JsonElement element when element.ValueKind == JsonValueKind.Object:
                return element.TryGetProperty("target", out var elementValue) ? elementValue.GetString() : null;
            case JsonDocument document when document.RootElement.ValueKind == JsonValueKind.Object:
                return document.RootElement.TryGetProperty("target", out var docValue) ? docValue.GetString() : null;
            case IDictionary<string, object?> dict:
                return dict.TryGetValue("target", out var dictValue) ? dictValue?.ToString() : null;
            case IDictionary<string, string> dictString:
                return dictString.TryGetValue("target", out var stringValue) ? stringValue : null;
            default:
                return null;
        }
    }

    private static bool IsDirectMessage(string? channel)
        => !string.IsNullOrWhiteSpace(channel) && channel.StartsWith("@", StringComparison.Ordinal);

    private static EntitySpec NormalizeEntity(EntitySpec entity)
    {
        var normalized = new EntitySpec
        {
            Id = entity.Id,
            Kind = entity.Kind,
            DisplayName = entity.DisplayName,
            Visibility = string.IsNullOrWhiteSpace(entity.Visibility) ? "team" : entity.Visibility,
            OwnerUserId = entity.OwnerUserId,
            Capabilities = entity.Capabilities ?? Array.Empty<string>(),
            Policy = entity.Policy ?? new PolicySpec()
        };

        normalized.Policy.AllowCommandsFrom = string.IsNullOrWhiteSpace(normalized.Policy.AllowCommandsFrom)
            ? "any"
            : normalized.Policy.AllowCommandsFrom;
        normalized.Policy.EnvWhitelist ??= Array.Empty<string>();

        return normalized;
    }

    private static void ValidateEntity(EntitySpec entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw ErrorFactory.HubBadRequest("INVALID_ENTITY_SPEC", "entity.id is required");
        }

        if (string.IsNullOrWhiteSpace(entity.Kind))
        {
            throw ErrorFactory.HubBadRequest("INVALID_ENTITY_SPEC", "entity.kind is required");
        }

        if (entity.Policy is null)
        {
            entity.Policy = new PolicySpec();
        }

        if (entity.Capabilities is null)
        {
            entity.Capabilities = Array.Empty<string>();
        }
    }

    private async Task PublishRoomState(string roomId)
    {
        var entities = _sessions.ListByRoom(roomId).Select(s => s.Entity).ToList();
        await _events.PublishAsync(roomId, "ROOM.STATE", new { entities });
    }

    private string? ResolveUserId()
    {
        var fromClaims = Context.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrWhiteSpace(fromClaims))
        {
            return fromClaims;
        }

        var http = Context.GetHttpContext();
        if (http?.Request.Headers.TryGetValue("X-User-Id", out var headerValue) == true && !string.IsNullOrWhiteSpace(headerValue))
        {
            return headerValue.ToString();
        }

        return null;
    }
}
