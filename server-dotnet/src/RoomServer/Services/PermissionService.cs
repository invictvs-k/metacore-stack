using RoomServer.Models;

namespace RoomServer.Services;

public sealed class PermissionService
{
    public bool CanSendCommand(EntitySession from, EntitySpec target)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(target);

        return target.Policy.AllowCommandsFrom switch
        {
            "any" => true,
            "orchestrator" => string.Equals(from.Entity.Kind, "orchestrator", StringComparison.OrdinalIgnoreCase),
            "owner" => target.OwnerUserId is not null && string.Equals(target.OwnerUserId, from.UserId, StringComparison.Ordinal),
            _ => false
        };
    }

    public bool CanAccessWorkspace(EntitySession session, string workspace, string? entityId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(workspace);

        return workspace switch
        {
            "room" => true,
            "entity" => string.Equals(entityId, session.Entity.Id, StringComparison.Ordinal),
            _ => false
        };
    }

    public bool CanDirectMessage(EntitySession from, EntitySpec to)
    {
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);

        return to.Visibility switch
        {
            "public" => true,
            "team" => true,
            "owner" => to.OwnerUserId is not null && string.Equals(to.OwnerUserId, from.UserId, StringComparison.Ordinal),
            _ => false
        };
    }

    public bool CanPromote(EntitySession session, string fromEntityId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(fromEntityId);

        if (string.Equals(session.Entity.Kind, "orchestrator", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(session.Entity.Id, fromEntityId, StringComparison.Ordinal);
    }
}
