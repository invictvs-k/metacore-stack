using System;

namespace RoomServer.Services.ArtifactStore;

public sealed class ArtifactStoreException : Exception
{
    public ArtifactStoreException(int statusCode, string error, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Error = error;
    }

    public int StatusCode { get; }

    public string Error { get; }
}
