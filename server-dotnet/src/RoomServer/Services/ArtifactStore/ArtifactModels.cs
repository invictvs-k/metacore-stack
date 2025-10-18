using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RoomServer.Services.ArtifactStore;

public interface IArtifactStore
{
    Task<ArtifactManifest> WriteAsync(ArtifactWriteRequest req, CancellationToken ct = default);
    Task<Stream> ReadAsync(ArtifactReadRequest req, CancellationToken ct = default);
    Task<IReadOnlyList<ArtifactManifest>> ListAsync(ArtifactListRequest req, CancellationToken ct = default);
    Task<ArtifactManifest> PromoteAsync(ArtifactPromoteRequest req, CancellationToken ct = default);
}

public record ArtifactWriteRequest(
    string RoomId,
    string Workspace,
    string? EntityId,
    string Name,
    string Type,
    Stream Data,
    long? Size,
    string? Port,
    IReadOnlyList<string>? Parents,
    Dictionary<string, object>? Metadata);

public record ArtifactReadRequest(string RoomId, string Workspace, string? EntityId, string Name);

public record ArtifactListRequest(
    string RoomId,
    string Workspace,
    string? EntityId,
    string? Prefix,
    string? Type,
    string? EntityFilter,
    DateTime? Since,
    int? Limit,
    int? Offset);

public record ArtifactPromoteRequest(
    string RoomId,
    string FromEntity,
    string Name,
    string? AsName,
    Dictionary<string, object>? Metadata);

public sealed class ArtifactManifest
{
    public string Name { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string Path { get; set; } = default!;
    public long Size { get; set; }
    public string Sha256 { get; set; } = default!;
    public Origin Origin { get; set; } = new();
    public int Version { get; set; } = 1;
    public List<string>? Parents { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime Ts { get; set; } = DateTime.UtcNow;
}

public sealed class Origin
{
    public string Room { get; set; } = default!;
    public string Entity { get; set; } = default!;
    public string? Port { get; set; }
    public string Workspace { get; set; } = "room";
    public string? Channel { get; set; }
}
