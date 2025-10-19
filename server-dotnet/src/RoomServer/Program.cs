using RoomServer.Controllers;
using RoomServer.Hubs;
using RoomServer.Models;
using RoomServer.Services;
using RoomServer.Services.ArtifactStore;
using RoomServer.Services.Mcp;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(policy =>
  {
    if (builder.Environment.IsDevelopment())
    {
      policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    }
    else
    {
      // Read allowed origins from configuration or environment variable
      var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
      if (allowedOrigins == null || allowedOrigins.Length == 0)
      {
        throw new InvalidOperationException("No allowed CORS origins configured for production.");
      }
      policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    }
  });
});
builder.Services.AddSingleton<RoomObservabilityService>();
builder.Services.AddSingleton<RoomEventPublisher>();
builder.Services.AddSingleton<IArtifactStore, FileArtifactStore>();
builder.Services.AddSingleton<SessionStore>();
builder.Services.AddSingleton<RoomContextStore>();
builder.Services.AddSingleton<PermissionService>();
builder.Services.AddSingleton<PolicyEngine>();
builder.Services.AddSingleton<McpRegistry>();
builder.Services.AddSingleton<McpConnectionManager>();
builder.Services.AddHostedService<McpRegistryHostedService>();
builder.Logging.AddConsole();

var app = builder.Build();

app.UseCors();
app.MapGet("/", () => Results.Text("RoomServer alive"));
app.MapHealthChecks("/health");
app.MapHub<RoomHub>("/room");
app.MapArtifactEndpoints();
app.MapMcpStatusEndpoints();
app.MapMcpAdminEndpoints();

app.Run();

public partial class Program;
