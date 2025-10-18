using RoomServer.Controllers;
using RoomServer.Hubs;
using RoomServer.Services;
using RoomServer.Services.ArtifactStore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<RoomEventPublisher>();
builder.Services.AddSingleton<IArtifactStore, FileArtifactStore>();
builder.Services.AddSingleton<SessionStore>();
builder.Services.AddSingleton<PermissionService>();
builder.Logging.AddConsole();

var app = builder.Build();

app.MapGet("/", () => Results.Text("RoomServer alive"));
app.MapHealthChecks("/health");
app.MapHub<RoomHub>("/room");
app.MapArtifactEndpoints();

app.Run();

public partial class Program;
