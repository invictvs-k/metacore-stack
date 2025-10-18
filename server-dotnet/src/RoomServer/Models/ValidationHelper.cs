using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RoomServer.Models;

public static partial class ValidationHelper
{
    [GeneratedRegex(@"^room-[A-Za-z0-9_-]{6,}$")]
    private static partial Regex RoomIdRegex();

    [GeneratedRegex(@"^E-[A-Za-z0-9_-]{2,64}$")]
    private static partial Regex EntityIdRegex();

    [GeneratedRegex(@"^[a-z][a-z0-9]*(\.[a-z0-9]+)*$")]
    private static partial Regex PortIdRegex();

    [GeneratedRegex(@"^[A-Z]+(\.[A-Z]+)*$")]
    private static partial Regex EventKindRegex();

    public static bool IsValidRoomId(string roomId)
    {
        return !string.IsNullOrWhiteSpace(roomId) && RoomIdRegex().IsMatch(roomId);
    }

    public static bool IsValidEntityId(string entityId)
    {
        return !string.IsNullOrWhiteSpace(entityId) && EntityIdRegex().IsMatch(entityId);
    }

    public static bool IsValidPortId(string portId)
    {
        return !string.IsNullOrWhiteSpace(portId) && PortIdRegex().IsMatch(portId);
    }

    public static bool IsValidEventKind(string kind)
    {
        return !string.IsNullOrWhiteSpace(kind) && EventKindRegex().IsMatch(kind);
    }

    public static bool IsValidEntityKind(string kind)
    {
        if (string.IsNullOrWhiteSpace(kind))
            return false;
        
        return kind.ToLowerInvariant() switch
        {
            "human" => true,
            "agent" => true,
            "npc" => true,
            "orchestrator" => true,
            _ => false
        };
    }

    public static bool ValidateChatPayload(object payload, out string? error)
    {
        error = null;
        
        try
        {
            if (payload is JsonElement element)
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    error = "Chat payload must be an object";
                    return false;
                }
                
                if (!element.TryGetProperty("text", out var textValue))
                {
                    error = "Chat payload must include 'text' field";
                    return false;
                }
                
                if (textValue.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(textValue.GetString()))
                {
                    error = "Chat payload 'text' must be a non-empty string";
                    return false;
                }
                
                return true;
            }
            
            if (payload is JsonDocument document)
            {
                return ValidateChatPayload(document.RootElement, out error);
            }
        }
        catch
        {
            error = "Invalid chat payload format";
            return false;
        }
        
        return true;
    }

    public static bool ValidateCommandPayload(object payload, out string? error)
    {
        error = null;
        
        try
        {
            if (payload is JsonElement element)
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    error = "Command payload must be an object";
                    return false;
                }
                
                if (!element.TryGetProperty("target", out var targetValue))
                {
                    error = "Command payload must include 'target' field";
                    return false;
                }
                
                if (!element.TryGetProperty("port", out var portValue))
                {
                    error = "Command payload must include 'port' field";
                    return false;
                }
                
                var targetStr = targetValue.GetString();
                var portStr = portValue.GetString();
                
                if (string.IsNullOrWhiteSpace(targetStr))
                {
                    error = "Command payload 'target' must be a non-empty string";
                    return false;
                }
                
                if (string.IsNullOrWhiteSpace(portStr))
                {
                    error = "Command payload 'port' must be a non-empty string";
                    return false;
                }
                
                return true;
            }
            
            if (payload is JsonDocument document)
            {
                return ValidateCommandPayload(document.RootElement, out error);
            }
        }
        catch
        {
            error = "Invalid command payload format";
            return false;
        }
        
        return true;
    }

    public static bool ValidateEventPayload(object payload, out string? error)
    {
        error = null;
        
        try
        {
            if (payload is JsonElement element)
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    error = "Event payload must be an object";
                    return false;
                }
                
                if (!element.TryGetProperty("kind", out var kindValue))
                {
                    error = "Event payload must include 'kind' field";
                    return false;
                }
                
                var kindStr = kindValue.GetString();
                
                if (string.IsNullOrWhiteSpace(kindStr))
                {
                    error = "Event payload 'kind' must be a non-empty string";
                    return false;
                }
                
                if (!IsValidEventKind(kindStr))
                {
                    error = "Event payload 'kind' must be in SCREAMING_CASE format (e.g., ENTITY.JOIN, ROOM.STATE)";
                    return false;
                }
                
                return true;
            }
            
            if (payload is JsonDocument document)
            {
                return ValidateEventPayload(document.RootElement, out error);
            }
        }
        catch
        {
            error = "Invalid event payload format";
            return false;
        }
        
        return true;
    }

    public static bool ValidateArtifactPayload(object payload, out string? error)
    {
        error = null;
        
        try
        {
            if (payload is JsonElement element)
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    error = "Artifact payload must be an object";
                    return false;
                }
                
                if (!element.TryGetProperty("manifest", out var manifestValue))
                {
                    error = "Artifact payload must include 'manifest' field";
                    return false;
                }
                
                if (manifestValue.ValueKind != JsonValueKind.Object)
                {
                    error = "Artifact payload 'manifest' must be an object";
                    return false;
                }
                
                return true;
            }
            
            if (payload is JsonDocument document)
            {
                return ValidateArtifactPayload(document.RootElement, out error);
            }
        }
        catch
        {
            error = "Invalid artifact payload format";
            return false;
        }
        
        return true;
    }
}
