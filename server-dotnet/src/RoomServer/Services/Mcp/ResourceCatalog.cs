using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using RoomServer.Models;

namespace RoomServer.Services.Mcp;

/// <summary>
/// Aggregates and resolves tools from all registered MCP servers.
/// </summary>
public class ResourceCatalog
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, (CatalogItem item, IMcpClient client)> _catalog = new();

    public ResourceCatalog(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a tool from an MCP server in the catalog.
    /// </summary>
    public void Register(string serverId, ToolSpec spec, IMcpClient client)
    {
        var key = $"{serverId}:{spec.id}";
        var item = new CatalogItem(serverId, spec.id, key, spec);
        _catalog[key] = (item, client);
        _logger.LogDebug("Registered tool: {Key}", key);
    }

    /// <summary>
    /// Lists all tools visible to the specified entity in the room.
    /// Applies visibility policies via the PolicyEngine.
    /// </summary>
    public IReadOnlyList<CatalogItem> ListVisible(string roomId, EntitySession session, PolicyEngine policyEngine)
    {
        var visibleTools = new List<CatalogItem>();

        foreach (var (item, _) in _catalog.Values)
        {
            if (policyEngine.CanList(session, item))
            {
                visibleTools.Add(item);
            }
        }

        return visibleTools;
    }

    /// <summary>
    /// Resolves a tool by its ID or full key.
    /// If the input contains ':', it's treated as a full key (serverId:toolId).
    /// Otherwise, it searches for the first tool with a matching toolId.
    /// </summary>
    public (CatalogItem item, IMcpClient client) Resolve(string toolIdOrKey)
    {
        // Try exact match first (full key)
        if (toolIdOrKey.Contains(':'))
        {
            if (_catalog.TryGetValue(toolIdOrKey, out var exact))
            {
                return exact;
            }
            throw new InvalidOperationException($"Tool not found: {toolIdOrKey}");
        }

        // Search by short tool ID
        var matches = _catalog.Values.Where(kvp => kvp.item.ToolId == toolIdOrKey).ToList();
        
        if (matches.Count == 0)
        {
            throw new InvalidOperationException($"Tool not found: {toolIdOrKey}");
        }

        if (matches.Count > 1)
        {
            _logger.LogWarning("Multiple tools found for ID '{ToolId}', using first match: {Key}", 
                toolIdOrKey, matches[0].item.Key);
        }

        return matches[0];
    }

    /// <summary>
    /// Gets all catalog items.
    /// </summary>
    public IReadOnlyList<CatalogItem> GetAll()
    {
        return _catalog.Values.Select(v => v.item).ToList();
    }
}
