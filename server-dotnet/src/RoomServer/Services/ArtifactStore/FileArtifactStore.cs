using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace RoomServer.Services.ArtifactStore;

public sealed class FileArtifactStore : IArtifactStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly string _rootPath;

    public FileArtifactStore(IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);
        _rootPath = Path.Combine(environment.ContentRootPath, ".ai-flow");
    }

    public async Task<ArtifactManifest> WriteAsync(ArtifactWriteRequest req, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        ValidateWorkspace(req.Workspace);
        ValidateName(req.Name);
        ArgumentException.ThrowIfNullOrWhiteSpace(req.Type);
        ArgumentNullException.ThrowIfNull(req.Data);
        if (req.Size.HasValue && req.Size.Value < 0)
        {
            throw new ArtifactStoreException(400, "InvalidArtifactSize", "Size must be non-negative");
        }

        if (req.Data.CanSeek)
        {
            req.Data.Seek(0, SeekOrigin.Begin);
        }

        var (relativeDir, physicalDir) = GetWorkspacePaths(req.RoomId, req.Workspace, req.EntityId);
        Directory.CreateDirectory(physicalDir);
        var manifestPath = Path.Combine(physicalDir, "manifest.json");
        var filePath = Path.Combine(physicalDir, req.Name);
        var tempPath = Path.Combine(physicalDir, $"{Guid.NewGuid():N}.tmp");

        var manifestLock = GetLock(manifestPath);
        await manifestLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var manifests = await LoadManifestsAsync(manifestPath, ct).ConfigureAwait(false);
            var nextVersion = manifests
                .Where(m => string.Equals(m.Name, req.Name, StringComparison.OrdinalIgnoreCase))
                .Select(m => m.Version)
                .DefaultIfEmpty(0)
                .Max() + 1;

            var metadata = req.Metadata is null ? null : new Dictionary<string, string>(req.Metadata);
            var parents = req.Parents is null ? null : req.Parents.ToList();

            await using (var destination = File.Create(tempPath))
            using (var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
            {
                var buffer = new byte[81920];
                long totalBytes = 0;
                int read;
                while ((read = await req.Data.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false)) > 0)
                {
                    await destination.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                    hash.AppendData(buffer, 0, read);
                    totalBytes += read;
                }

                await destination.FlushAsync(ct).ConfigureAwait(false);

                if (req.Size.HasValue && req.Size.Value != totalBytes)
                {
                    throw new ArtifactStoreException(400, "InvalidArtifactSize", "Provided size does not match stream length");
                }

                var shaBytes = hash.GetHashAndReset();
                var sha = Convert.ToHexString(shaBytes).ToLowerInvariant();

                var manifest = new ArtifactManifest
                {
                    Name = req.Name,
                    Type = req.Type,
                    Path = CombineRelative(relativeDir, req.Name),
                    Size = totalBytes,
                    Sha256 = sha,
                    Origin = new Origin
                    {
                        Room = req.RoomId,
                        Entity = string.IsNullOrWhiteSpace(req.EntityId) ? "E-UNKNOWN" : req.EntityId!,
                        Port = req.Port,
                        Workspace = req.Workspace
                    },
                    Version = nextVersion,
                    Parents = parents,
                    Metadata = metadata,
                    Ts = DateTime.UtcNow
                };

                File.Move(tempPath, filePath, overwrite: true);

                manifests.Add(manifest);
                await WriteManifestsAsync(manifestPath, manifests, ct).ConfigureAwait(false);
                return manifest;
            }
        }
        finally
        {
            manifestLock.Release();
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    public Task<Stream> ReadAsync(ArtifactReadRequest req, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        ValidateWorkspace(req.Workspace);
        ValidateName(req.Name);

        var (_, physicalDir) = GetWorkspacePaths(req.RoomId, req.Workspace, req.EntityId);
        var filePath = Path.Combine(physicalDir, req.Name);
        if (!File.Exists(filePath))
        {
            throw new ArtifactStoreException(404, "ArtifactNotFound", "Artifact not found");
        }

        Stream stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult(stream);
    }

    public async Task<IReadOnlyList<ArtifactManifest>> ListAsync(ArtifactListRequest req, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        ValidateWorkspace(req.Workspace);

        var (_, physicalDir) = GetWorkspacePaths(req.RoomId, req.Workspace, req.EntityId);
        var manifestPath = Path.Combine(physicalDir, "manifest.json");
        var manifestLock = GetLock(manifestPath);
        await manifestLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var manifests = await LoadManifestsAsync(manifestPath, ct).ConfigureAwait(false);
            if (manifests.Count == 0)
            {
                return Array.Empty<ArtifactManifest>();
            }

            IEnumerable<ArtifactManifest> filteredManifests = manifests;

            if (!string.IsNullOrWhiteSpace(req.EntityFilter))
            {
                filteredManifests = filteredManifests
                    .Where(m => string.Equals(m.Origin?.Entity, req.EntityFilter, StringComparison.OrdinalIgnoreCase));
            }

            var latest = new Dictionary<(string Name, string Entity), ArtifactManifest>(ManifestKeyComparer.Instance);
            foreach (var manifest in filteredManifests)
            {
                var entity = manifest.Origin?.Entity ?? string.Empty;
                var key = (manifest.Name, entity);

                if (!latest.TryGetValue(key, out var existing) ||
                    (manifest.Version != null && (existing.Version == null || manifest.Version > existing.Version)) ||
                    (manifest.Version == null && existing.Version == null && manifest.Ts > existing.Ts) ||
                    (manifest.Version != null && existing.Version != null && manifest.Version == existing.Version && manifest.Ts > existing.Ts))
                {
                    latest[key] = manifest;
                }
            }

            IEnumerable<ArtifactManifest> query = latest.Values;

            if (!string.IsNullOrWhiteSpace(req.Prefix))
            {
                query = query.Where(m => m.Name.StartsWith(req.Prefix, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(req.Type))
            {
                query = query.Where(m => string.Equals(m.Type, req.Type, StringComparison.OrdinalIgnoreCase));
            }

            if (req.Since.HasValue)
            {
                var sinceUtc = DateTime.SpecifyKind(req.Since.Value, DateTimeKind.Utc);
                query = query.Where(m => m.Ts >= sinceUtc);
            }

            query = query.OrderByDescending(m => m.Ts);

            if (req.Offset.HasValue && req.Offset.Value > 0)
            {
                query = query.Skip(req.Offset.Value);
            }

            if (req.Limit.HasValue && req.Limit.Value > 0)
            {
                query = query.Take(req.Limit.Value);
            }

            return query.ToList();
        }
        finally
        {
            manifestLock.Release();
        }
    }

    public async Task<ArtifactManifest> PromoteAsync(ArtifactPromoteRequest req, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(req);
        ArgumentException.ThrowIfNullOrWhiteSpace(req.FromEntity);
        ValidateName(req.Name);
        if (!string.IsNullOrWhiteSpace(req.AsName))
        {
            ValidateName(req.AsName!);
        }

        var (entityRelativeDir, entityPhysicalDir) = GetWorkspacePaths(req.RoomId, "entity", req.FromEntity);
        var entityManifestPath = Path.Combine(entityPhysicalDir, "manifest.json");
        var manifestLock = GetLock(entityManifestPath);
        List<ArtifactManifest> entityManifests;
        await manifestLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            entityManifests = await LoadManifestsAsync(entityManifestPath, ct).ConfigureAwait(false);
        }
        finally
        {
            manifestLock.Release();
        }

        if (entityManifests.Count == 0)
        {
            throw new ArtifactStoreException(404, "ArtifactNotFound", "Artifact not found in entity workspace");
        }

        var sourceManifest = entityManifests
            .Where(m => string.Equals(m.Name, req.Name, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => m.Version)
            .FirstOrDefault();

        if (sourceManifest is null)
        {
            throw new ArtifactStoreException(404, "ArtifactNotFound", "Artifact not found in entity workspace");
        }

        var sourcePath = Path.Combine(entityPhysicalDir, req.Name);
        if (!File.Exists(sourcePath))
        {
            throw new ArtifactStoreException(404, "ArtifactNotFound", "Artifact file missing in entity workspace");
        }

        await using var stream = File.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var parents = new List<string> { sourceManifest.Sha256 };
        var metadata = req.Metadata is null ? null : new Dictionary<string, string>(req.Metadata);
        var writeReq = new ArtifactWriteRequest(
            req.RoomId,
            "room",
            req.FromEntity,
            req.AsName ?? req.Name,
            sourceManifest.Type,
            stream,
            sourceManifest.Size,
            sourceManifest.Origin.Port,
            parents,
            metadata);

        return await WriteAsync(writeReq, ct).ConfigureAwait(false);
    }

    private (string relativeDir, string physicalDir) GetWorkspacePaths(string roomId, string workspace, string? entityId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(roomId);
        ArgumentNullException.ThrowIfNull(workspace);
        workspace = workspace.ToLowerInvariant();

        workspace = workspace switch
        {
            "room" => "room",
            "entity" => "entity",
            _ => throw new ArtifactStoreException(400, "InvalidWorkspace", $"Unsupported workspace '{workspace}'")
        };

        var baseRelative = Path.Combine(".ai-flow", "runs", roomId);
        var basePhysical = Path.Combine(_rootPath, "runs", roomId);

        if (workspace == "room")
        {
            return (Path.Combine(baseRelative, "artifacts"), Path.Combine(basePhysical, "artifacts"));
        }

        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArtifactStoreException(400, "MissingEntity", "EntityId is required for entity workspace");
        }

        return (
            Path.Combine(baseRelative, "entities", entityId, "artifacts"),
            Path.Combine(basePhysical, "entities", entityId, "artifacts"));
    }

    private SemaphoreSlim GetLock(string path)
    {
        return _locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
    }

    private static async Task<List<ArtifactManifest>> LoadManifestsAsync(string path, CancellationToken ct)
    {
        if (!File.Exists(path))
        {
            return new List<ArtifactManifest>();
        }

        await using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var manifests = await JsonSerializer.DeserializeAsync<List<ArtifactManifest>>(stream, SerializerOptions, ct)
            .ConfigureAwait(false);
        return manifests ?? new List<ArtifactManifest>();
    }

    private static async Task WriteManifestsAsync(string path, List<ArtifactManifest> manifests, CancellationToken ct)
    {
        await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, manifests, SerializerOptions, ct).ConfigureAwait(false);
    }

    private static string CombineRelative(string relativeDir, string name)
    {
        var path = Path.Combine(relativeDir, name);
        return path.Replace('\\', '/');
    }

    private static void ValidateWorkspace(string workspace)
    {
        if (!string.Equals(workspace, "room", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(workspace, "entity", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArtifactStoreException(400, "InvalidWorkspace", $"Unsupported workspace '{workspace}'");
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArtifactStoreException(400, "InvalidArtifactName", "name is required");
        }

        if (name.Contains("..", StringComparison.Ordinal) || name.Contains('/') || name.Contains('\\'))
        {
            throw new ArtifactStoreException(400, "InvalidArtifactName", "name contains invalid path tokens");
        }
    }

    private sealed class ManifestKeyComparer : IEqualityComparer<(string Name, string Entity)>
    {
        public static ManifestKeyComparer Instance { get; } = new();

        public bool Equals((string Name, string Entity) x, (string Name, string Entity) y)
        {
            return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(x.Entity, y.Entity, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode((string Name, string Entity) obj)
        {
            var nameHash = obj.Name is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Name);
            var entityHash = obj.Entity is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Entity);
            return HashCode.Combine(nameHash, entityHash);
        }
    }
}
