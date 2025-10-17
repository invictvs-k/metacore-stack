using Microsoft.AspNetCore.SignalR;

namespace RoomServer.Hubs;

public class RoomHub : Hub
{
  public async Task Join(string roomId, object entity)
  {
    _ = entity;
    await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
  }

  public async Task Leave(string roomId) =>
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);

  public async Task SendToRoom(string roomId, object message) =>
    await Clients.Group(roomId).SendAsync("message", message);
}
