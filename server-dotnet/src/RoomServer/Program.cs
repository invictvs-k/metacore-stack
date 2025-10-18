using Microsoft.AspNetCore.SignalR;
using RoomServer.Hubs;
using RoomServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<RoomManager>();
builder.Services.AddSingleton<RoomEventPublisher>();
builder.Logging.AddConsole();

var app = builder.Build();

app.MapGet("/", () => Results.Text("RoomServer alive"));
app.MapHealthChecks("/health");
app.MapHub<RoomHub>("/room");

app.Run();

public partial class Program;
