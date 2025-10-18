namespace RoomServer.Models;

public sealed record ErrorResponse(string Error, string Code, string Message);
