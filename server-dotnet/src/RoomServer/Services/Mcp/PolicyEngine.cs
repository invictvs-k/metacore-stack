using System;
using Microsoft.Extensions.Logging;
using RoomServer.Models;

namespace RoomServer.Services.Mcp;

/// <summary>
/// Validates permissions for tool listing and invocation based on policies.
/// </summary>
public class PolicyEngine
{
  private readonly ILogger<PolicyEngine> _logger;

  public PolicyEngine(ILogger<PolicyEngine> logger)
  {
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  /// <summary>
  /// Checks if an entity can list a tool based on visibility policy.
  /// For MVP: returns true if visibility is not "entity" (private).
  /// </summary>
  public bool CanList(EntitySession session, CatalogItem item)
  {
    var visibility = item.Spec.policy?.visibility ?? "room";

    // "entity" visibility means private/hidden
    if (string.Equals(visibility, "entity", StringComparison.OrdinalIgnoreCase))
    {
      _logger.LogDebug("Tool {Key} has entity visibility, not listable", item.Key);
      return false;
    }

    return true;
  }

  /// <summary>
  /// Checks if an entity can call a tool based on allowedEntities policy.
  /// Supports: "public" (anyone), "team" (anyone in the room), "owner" (tool owner only).
  /// For MVP, "team" means anyone in the room.
  /// </summary>
  public bool CanCall(EntitySession session, CatalogItem item)
  {
    var allowedEntities = item.Spec.policy?.allowedEntities ?? "public";

    switch (allowedEntities.ToLowerInvariant())
    {
      case "public":
        return true;

      case "team":
        // For MVP, "team" means anyone who is in the room
        // Session exists means they're in the room
        return true;

      case "owner":
        // For MVP, we don't have ownership tracking yet
        // Return false for now, can be enhanced in future
        _logger.LogWarning("Tool {Key} requires owner permission, denying for MVP", item.Key);
        return false;

      default:
        _logger.LogWarning("Unknown allowedEntities policy: {Policy}, denying access", allowedEntities);
        return false;
    }
  }

  /// <summary>
  /// Checks rate limiting for tool calls.
  /// For MVP, always returns true. Real implementation will be in Item 7.
  /// </summary>
  public bool CheckRateLimit(EntitySession session, CatalogItem item)
  {
    // TODO: Implement actual rate limiting in Item 7
    return true;
  }
}
