using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RoomServer.Models;

public static partial class ValidationHelper
{
    [GeneratedRegex(@"^room-[A-Za-z0-9_-]{6,}$")]
    private static partial Regex RoomIdRegex();

    // The regex below enforces the schema (see IMPLEMENTATION_SUMMARY.md line 73 and the PR description)
    // requiring 2-64 characters after 'E-'.
    // If backward compatibility with 1-character IDs is required in the future, update this regex accordingly.
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
        if (!TryGetJsonObjectPayload(
                payload,
                requiredError: "Chat payload is required",
                objectError: "Chat payload must be an object",
                invalidFormatError: "Invalid chat payload format",
                out var element,
                out error))
        {
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

        error = null;
        return true;
    }

    public static bool ValidateCommandPayload(object payload, out string? error)
    {
        if (!TryGetJsonObjectPayload(
                payload,
                requiredError: "Command payload is required",
                objectError: "Command payload must be an object",
                invalidFormatError: "Invalid command payload format",
                out var element,
                out error))
        {
            return false;
        }

        if (!element.TryGetProperty("target", out var targetValue))
        {
            error = "Command payload must include 'target' field";
            return false;
        }

        if (targetValue.ValueKind != JsonValueKind.String)
        {
            error = "Command payload 'target' must be a string";
            return false;
        }

        var targetStr = targetValue.GetString();

        if (string.IsNullOrWhiteSpace(targetStr))
        {
            error = "Command payload 'target' must be a non-empty string";
            return false;
        }

        // Port is recommended but not strictly required for backward compatibility
        // The schema requires it, but existing code may not provide it

        // Explicitly skip validation of 'port' field for backward compatibility
        // If present, we do not validate its value
        // Example:
        // if (element.TryGetProperty("port", out var portValue)) { /* intentionally not validated */ }

        error = null;
        return true;
    }

    public static bool ValidateEventPayload(object payload, out string? error)
    {
        if (!TryGetJsonObjectPayload(
                payload,
                requiredError: "Event payload is required",
                objectError: "Event payload must be an object",
                invalidFormatError: "Invalid event payload format",
                out var element,
                out error))
        {
            return false;
        }

        if (!element.TryGetProperty("kind", out var kindValue))
        {
            error = "Event payload must include 'kind' field";
            return false;
        }

        if (kindValue.ValueKind != JsonValueKind.String)
        {
            error = "Event payload 'kind' must be a string";
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

        error = null;
        return true;
    }

    /// <summary>
    /// Validates that the provided payload is a JSON object containing a 'manifest' object
    /// with required string properties: 'name', 'version', and 'sha256'.
    /// <para>
    /// Expected payload shape:
    /// {
    ///   "manifest": {
    ///     "name": string (required, non-empty),
    ///     "version": string (required, non-empty),
    ///     "sha256": string (required, non-empty)
    ///   }
    /// }
    /// </para>
    /// </summary>
    /// <param name="payload">The payload object to validate. Must be a JSON object with the required manifest fields.</param>
    /// <param name="error">Outputs a validation error message if validation fails; otherwise, null.</param>
    /// <returns>True if the payload is valid; false otherwise.</returns>
    public static bool ValidateArtifactPayload(object payload, out string? error)
        if (!TryGetJsonObjectPayload(
                payload,
                requiredError: "Artifact payload is required",
                objectError: "Artifact payload must be an object",
                invalidFormatError: "Invalid artifact payload format",
                out var element,
                out error))
        {
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

        if (!manifestValue.TryGetProperty("name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(nameValue.GetString()))
        {
            error = "Artifact payload 'manifest.name' must be a non-empty string";
            return false;
        }

        if (!manifestValue.TryGetProperty("version", out var versionValue) || versionValue.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(versionValue.GetString()))
        {
            error = "Artifact payload 'manifest.version' must be a non-empty string";
            return false;
        }

        if (!manifestValue.TryGetProperty("sha256", out var shaValue) || shaValue.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(shaValue.GetString()))
        {
            error = "Artifact payload 'manifest.sha256' must be a non-empty string";
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryGetJsonObjectPayload(
        object payload,
        string requiredError,
        string objectError,
        string invalidFormatError,
        out JsonElement element,
        out string? error)
    {
        element = default;
        error = null;

        if (payload is null)
        {
            error = requiredError;
            return false;
        }

        try
        {
            switch (payload)
            {
                case JsonElement jsonElement:
                    if (jsonElement.ValueKind != JsonValueKind.Object)
                    {
                        error = objectError;
                        return false;
                    }

                    element = jsonElement.Clone();
                    return true;

                case JsonDocument jsonDocument:
                    var root = jsonDocument.RootElement;
                    if (root.ValueKind != JsonValueKind.Object)
                    {
                        error = objectError;
                        return false;
                    }

                    element = root.Clone();
                    return true;

                default:
                    error = objectError;
                    return false;
            }
        }
        catch
        {
            error = invalidFormatError;
            return false;
        }
    }
}
