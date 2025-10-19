using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RoomOperator.Abstractions;
using RoomOperator.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace RoomOperator.HttpApi;

public static class OperatorHttpApi
{
    public static void MapOperatorEndpoints(this WebApplication app)
    {
        // POST /apply - Apply a RoomSpec
        app.MapPost("/apply", async (
            [FromBody] ApplyRequestDto requestDto,
            [FromServices] RoomOperatorService operatorService,
            [FromServices] ILogger<Program> logger,
            HttpContext context,
            CancellationToken ct) =>
        {
            var dryRun = context.Request.Headers["X-Dry-Run"].ToString() == "true";
            var confirm = context.Request.Headers["X-Confirm"].ToString() == "true";
            
            logger.LogInformation("Received /apply request (dryRun={DryRun}, confirm={Confirm})", dryRun, confirm);
            
            var request = new ApplyRequest
            {
                Spec = requestDto.Spec,
                DryRun = dryRun,
                Confirm = confirm
            };
            
            var result = await operatorService.ApplySpecAsync(request, ct);
            
            if (!result.Success && result.Warnings.Any(w => w.Contains("queued")))
            {
                return Results.Accepted("/status", new { message = "Request queued", correlationId = result.CorrelationId });
            }
            
            if (!result.Success)
            {
                return Results.BadRequest(new { errors = result.Errors, warnings = result.Warnings });
            }
            
            return Results.Ok(new
            {
                success = result.Success,
                partialSuccess = result.PartialSuccess,
                correlationId = result.CorrelationId,
                phase = result.LastCompletedPhase.ToString(),
                diff = result.Diff,
                warnings = result.Warnings,
                duration = (result.EndTime - result.StartTime).TotalSeconds
            });
        });
        
        // GET /status - Get operator status
        app.MapGet("/status", ([FromServices] RoomOperatorService operatorService) =>
        {
            var status = operatorService.GetStatus();
            return Results.Ok(status);
        });
        
        // GET /status/rooms/{roomId} - Get room-specific status
        app.MapGet("/status/rooms/{roomId}", (
            [FromRoute] string roomId,
            [FromServices] RoomOperatorService operatorService) =>
        {
            var status = operatorService.GetRoomStatus(roomId);
            
            if (status == null)
            {
                return Results.NotFound(new { error = $"Room {roomId} not found" });
            }
            
            return Results.Ok(status);
        });
        
        // GET /health - Health check
        app.MapGet("/health", ([FromServices] RoomOperatorService operatorService) =>
        {
            var status = operatorService.GetStatus();
            
            return status.Health switch
            {
                HealthStatus.Healthy => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }),
                HealthStatus.Degraded => Results.Ok(new { status = "degraded", timestamp = DateTime.UtcNow }),
                _ => Results.Problem(title: "Unhealthy", statusCode: 503)
            };
        });
        
        // GET /audit - Get audit log
        app.MapGet("/audit", (
            [FromServices] AuditLog auditLog,
            [FromQuery] int? count,
            [FromQuery] string? correlationId) =>
        {
            if (!string.IsNullOrEmpty(correlationId))
            {
                var entries = auditLog.GetByCorrelation(correlationId);
                return Results.Ok(new { entries });
            }
            
            var recentEntries = auditLog.GetRecent(count ?? 100);
            return Results.Ok(new { entries = recentEntries });
        });
        
        // POST /mcp/load - Load MCP providers on RoomServer
        app.MapPost("/mcp/load", async (
            [FromBody] LoadMcpProvidersRequestDto requestDto,
            [FromServices] IMcpClient mcpClient,
            [FromServices] ILogger<Program> logger,
            CancellationToken ct) =>
        {
            logger.LogInformation("Received /mcp/load request with {Count} providers", requestDto.Providers.Length);
            
            try
            {
                await mcpClient.LoadMcpProvidersAsync(requestDto.Providers, ct);
                return Results.Ok(new { message = "MCP providers loaded", count = requestDto.Providers.Length });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load MCP providers");
                return Results.Problem(title: "Failed to load MCP providers", detail: ex.Message, statusCode: 500);
            }
        });
        
        // GET /mcp/status - Get MCP status from RoomServer
        app.MapGet("/mcp/status", async (
            [FromServices] IMcpClient mcpClient,
            [FromServices] ILogger<Program> logger,
            CancellationToken ct) =>
        {
            logger.LogDebug("Received /mcp/status request");
            
            try
            {
                var status = await mcpClient.GetMcpStatusAsync(ct);
                return Results.Ok(status);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get MCP status");
                return Results.Problem(title: "Failed to get MCP status", detail: ex.Message, statusCode: 500);
            }
        });
    }
}

public sealed class ApplyRequestDto
{
    public RoomSpec Spec { get; set; } = default!;
}

public sealed class LoadMcpProvidersRequestDto
{
    public required McpProviderConfig[] Providers { get; set; }
}
