using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using RoomServer.Services.ArtifactStore;
using Xunit;

namespace RoomServer.Tests;

public sealed class FileArtifactStoreTests : IAsyncLifetime
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), "FileArtifactStoreTests", Guid.NewGuid().ToString("N"));
    private FileArtifactStore _store = default!;

    [Fact]
    public async Task WriteAsync_PersistsArtifactAndManifest()
    {
        await using var stream = CreateStream("hello world");
        var request = new ArtifactWriteRequest(
            "room-a",
            "room",
            "E-01",
            "note.txt",
            "text/plain",
            stream,
            stream.Length,
            "text.generate",
            new[] { "abc123" },
            new() { { "stage", "draft" } });

        var manifest = await _store.WriteAsync(request);

        manifest.Version.Should().Be(1);
        manifest.Origin.Workspace.Should().Be("room");
        manifest.Path.Should().Be(".ai-flow/runs/room-a/artifacts/note.txt");
        manifest.Parents.Should().ContainSingle().Which.Should().Be("abc123");
        manifest.Metadata.Should().ContainKey("stage").WhoseValue.Should().Be("draft");

        var artifactPath = Path.Combine(_tempRoot, ".ai-flow", "runs", "room-a", "artifacts", "note.txt");
        File.Exists(artifactPath).Should().BeTrue();
        var storedContent = await File.ReadAllTextAsync(artifactPath);
        storedContent.Should().Be("hello world");
    }

    [Fact]
    public async Task WriteAsync_IncrementsVersion()
    {
        await using var first = CreateStream("v1");
        var req1 = new ArtifactWriteRequest("room-b", "room", "E-01", "story.md", "text/markdown", first, first.Length, null, null, null);
        var manifest1 = await _store.WriteAsync(req1);

        await using var second = CreateStream("v2");
        var req2 = new ArtifactWriteRequest("room-b", "room", "E-01", "story.md", "text/markdown", second, second.Length, null, null, null);
        var manifest2 = await _store.WriteAsync(req2);

        manifest1.Version.Should().Be(1);
        manifest2.Version.Should().Be(2);

        var list = await _store.ListAsync(new ArtifactListRequest("room-b", "room", null, null, null, null, null, null, null));
        list.Should().ContainSingle();
        list[0].Version.Should().Be(2);
        list[0].Sha256.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task PromoteAsync_CopiesArtifactToRoomWorkspace()
    {
        await using var draftStream = CreateStream("draft content");
        var draftRequest = new ArtifactWriteRequest("room-c", "entity", "E-42", "draft.md", "text/markdown", draftStream, draftStream.Length, null, null, null);
        var entityManifest = await _store.WriteAsync(draftRequest);

        var promoted = await _store.PromoteAsync(new ArtifactPromoteRequest("room-c", "E-42", "draft.md", "draft-final.md", new() { { "promotedBy", "E-42" } }));

        promoted.Name.Should().Be("draft-final.md");
        promoted.Origin.Workspace.Should().Be("room");
        promoted.Origin.Entity.Should().Be("E-42");
        promoted.Parents.Should().ContainSingle().Which.Should().Be(entityManifest.Sha256);

        var roomArtifactPath = Path.Combine(_tempRoot, ".ai-flow", "runs", "room-c", "artifacts", "draft-final.md");
        File.Exists(roomArtifactPath).Should().BeTrue();
        var promotedContent = await File.ReadAllTextAsync(roomArtifactPath);
        promotedContent.Should().Be("draft content");
    }

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_tempRoot);
        var environment = new TestHostEnvironment(_tempRoot);
        _store = new FileArtifactStore(environment);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }

        return Task.CompletedTask;
    }

    private static MemoryStream CreateStream(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        return new MemoryStream(bytes, writable: false);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string rootPath)
        {
            ContentRootPath = rootPath;
            EnvironmentName = Environments.Development;
            ApplicationName = "RoomServer.Tests";
            ContentRootFileProvider = new PhysicalFileProvider(rootPath);
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
