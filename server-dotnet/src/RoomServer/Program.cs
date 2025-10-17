using Microsoft.AspNetCore.SignalR;
using RoomServer.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapGet("/", () => Results.Text("RoomServer alive"));
app.MapHealthChecks("/health");
app.MapHub<RoomHub>("/room");

app.Run();
