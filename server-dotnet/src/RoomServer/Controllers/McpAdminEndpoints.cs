using Microsoft.AspNetCore.Mvc;
using RoomServer.Models;
using RoomServer.Services.Mcp;

namespace RoomServer.Controllers;

/// <summary>
/// Admin endpoints for MCP provider management.
/// These endpoints allow external systems (like RoomOperator) to control MCP connections.
/// </summary>
public static class McpAdminEndpoints
{
    public static void MapMcpAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/admin/mcp");

        // Load and connect to MCP providers
        group.MapPost("/load", async (
            [FromBody] LoadMcpProvidersRequest request,
            [FromServices] McpConnectionManager manager) =>
        {
            if (request.Providers == null || request.Providers.Length == 0)
            {
                return Results.BadRequest(new { error = "No providers specified" });
            }

            manager.LoadProviderConfigs(request.Providers);
            await manager.ConnectProvidersAsync();

            return Results.Ok(new
            {
                message = "MCP providers loading initiated",
                count = request.Providers.Length
            });
        });

        // Get MCP connection status
        group.MapGet("/status", ([FromServices] McpConnectionManager manager) =>
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

/// <summary>
/// Request model for loading MCP providers
/// </summary>
public record LoadMcpProvidersRequest(McpServerConfig[] Providers);
