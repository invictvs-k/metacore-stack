using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using RoomServer.Models;

namespace RoomServer.Services;

public static class ErrorFactory
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static HubException HubUnauthorized(string code, string message)
        => CreateHubException(401, code, message);

    public static HubException HubForbidden(string code, string message)
        => CreateHubException(403, code, message);

    public static HubException HubBadRequest(string code, string message)
        => CreateHubException(400, code, message);

    public static HubException HubNotFound(string code, string message)
        => CreateHubException(404, code, message);

    public static IResult HttpUnauthorized(string code, string message)
        => CreateHttpResult(401, code, message);

    public static IResult HttpForbidden(string code, string message)
        => CreateHttpResult(403, code, message);

    public static IResult HttpBadRequest(string code, string message)
        => CreateHttpResult(400, code, message);

    public static IResult HttpNotFound(string code, string message)
        => CreateHttpResult(404, code, message);

    private static HubException CreateHubException(int statusCode, string code, string message)
    {
        var error = new ErrorResponse(MapError(statusCode), code, message);
        return new HubException(JsonSerializer.Serialize(error, SerializerOptions));
    }

    private static IResult CreateHttpResult(int statusCode, string code, string message)
    {
        var error = new ErrorResponse(MapError(statusCode), code, message);
        return Results.Json(error, statusCode: statusCode);
    }

    private static string MapError(int statusCode)
        => statusCode switch
        {
            400 => "BadRequest",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "NotFound",
            _ => "Error"
        };
}
