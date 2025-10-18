using RoomServer.Controllers;
using RoomServer.Hubs;
using RoomServer.Services;
using RoomServer.Services.ArtifactStore;
using RoomServer.Services.Mcp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<RoomEventPublisher>();
builder.Services.AddSingleton<IArtifactStore, FileArtifactStore>();
builder.Services.AddSingleton<SessionStore>();
builder.Services.AddSingleton<PermissionService>();
builder.Services.AddSingleton<PolicyEngine>();
builder.Services.AddSingleton<McpRegistry>();
builder.Logging.AddConsole();

var app = builder.Build();

// Initialize MCP Registry asynchronously in background
var mcpRegistry = app.Services.GetRequiredService<McpRegistry>();
_ = Task.Run(async () =>
{
    try
    {
        await mcpRegistry.InitializeAsync();
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to initialize MCP Registry");
    }
});

app.MapGet("/", () => Results.Text("RoomServer alive"));
app.MapHealthChecks("/health");
app.MapHub<RoomHub>("/room");
app.MapArtifactEndpoints();

app.Run();

public partial class Program;
