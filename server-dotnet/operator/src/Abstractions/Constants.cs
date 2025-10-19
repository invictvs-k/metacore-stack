namespace RoomOperator.Abstractions;

/// <summary>
/// Application-wide constants used across the RoomOperator.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Default value for DM visibility policy when not specified.
    /// </summary>
    public const string DefaultDmVisibility = "team";
    
    /// <summary>
    /// Placeholder token value used in configuration files that should be replaced at runtime.
    /// </summary>
    public const string AuthTokenPlaceholder = "${ROOM_AUTH_TOKEN}";
}
