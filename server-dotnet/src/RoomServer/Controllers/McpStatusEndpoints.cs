using Microsoft.AspNetCore.Mvc;
using RoomServer.Services.Mcp;

namespace RoomServer.Controllers;

/// <summary>
/// Public status endpoints for monitoring MCP provider health.
/// </summary>
public static class McpStatusEndpoints
{
    public static void MapMcpStatusEndpoints(this IEndpointRouteBuilder app)
    {
        // Get MCP connection status (public, read-only)
        app.MapGet("/status/mcp", ([FromServices] McpConnectionManager manager) =>
        {
            var status = manager.GetStatus();
            return Results.Ok(new
            {
                providers = status.Select(s => new
                {
                    id = s.Id,
                    state = s.State.ToString().ToLowerInvariant(),
                    attempts = s.Attempts,
                    lastChangeAt = s.LastChangeAt,
                    lastError = s.LastError,
                    nextRetryAt = s.NextRetryAt
                })
            });
        });
    }
}
