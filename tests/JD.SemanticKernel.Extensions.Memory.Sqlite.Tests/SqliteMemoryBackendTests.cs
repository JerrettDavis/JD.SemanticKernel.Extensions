using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace JD.SemanticKernel.Extensions.Memory.Sqlite.Tests;

public class SqliteMemoryBackendTests : IDisposable
{
    private static readonly float[] HalfEmbedding = [0.5f];
    private readonly SqliteMemoryBackend _backend;

    public SqliteMemoryBackendTests()
    {
        // Use in-memory SQLite for tests
        _backend = new SqliteMemoryBackend("Data Source=:memory:");
    }

    public void Dispose()
    {
        _backend.Dispose();
        GC.SuppressFinalize(this);
    }

    private static MemoryRecord CreateRecord(string id, float[] embedding, Dictionary<string, string>? metadata = null) => new()
    {
        Id = id,
        Text = $"Text for {id}",
        Embedding = embedding,
        Metadata = metadata ?? new Dictionary<string, string>(StringComparer.Ordinal),
        CreatedAt = DateTimeOffset.UtcNow,
        LastAccessedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task StoreAndGet_RoundTrips()
    {
        var record = CreateRecord("r1", [1.0f, 0.0f, 0.0f]);
        await _backend.StoreAsync(record);

        var result = await _backend.GetAsync("r1");

        Assert.NotNull(result);
        Assert.Equal("r1", result!.Id);
        Assert.Equal("Text for r1", result.Text);
    }

    [Fact]
    public async Task StoreAndGet_PreservesEmbedding()
    {
        float[] embedding = [0.1f, 0.2f, 0.3f, 0.4f];
        var record = CreateRecord("r1", embedding);
        await _backend.StoreAsync(record);

        var result = await _backend.GetAsync("r1");

        Assert.NotNull(result);
        var resultEmbedding = result!.Embedding.ToArray();
        Assert.Equal(embedding.Length, resultEmbedding.Length);
        for (var i = 0; i < embedding.Length; i++)
        {
            Assert.Equal(embedding[i], resultEmbedding[i], precision: 5);
        }
    }

    [Fact]
    public async Task StoreAndGet_PreservesMetadata()
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["source"] = "test",
            ["category"] = "unit-test",
        };
        var record = CreateRecord("r1", [1.0f], metadata);
        await _backend.StoreAsync(record);

        var result = await _backend.GetAsync("r1");

        Assert.NotNull(result);
        Assert.Equal("test", result!.Metadata["source"]);
        Assert.Equal("unit-test", result.Metadata["category"]);
    }

    [Fact]
    public async Task Exists_WorksCorrectly()
    {
        await _backend.StoreAsync(CreateRecord("r1", [1.0f]));

        Assert.True(await _backend.ExistsAsync("r1"));
        Assert.False(await _backend.ExistsAsync("nonexistent"));
    }

    [Fact]
    public async Task Delete_RemovesRecord()
    {
        await _backend.StoreAsync(CreateRecord("r1", [1.0f]));
        await _backend.DeleteAsync("r1");

        Assert.False(await _backend.ExistsAsync("r1"));
        Assert.Null(await _backend.GetAsync("r1"));
    }

    [Fact]
    public async Task Search_ReturnsSortedByScore()
    {
        await _backend.StoreAsync(CreateRecord("close", [1.0f, 0.0f, 0.0f]));
        await _backend.StoreAsync(CreateRecord("far", [0.0f, 1.0f, 0.0f]));

        var results = await _backend.SearchAsync(new ReadOnlyMemory<float>([1.0f, 0.0f, 0.0f]), topK: 2);

        Assert.Equal(2, results.Count);
        Assert.Equal("close", results[0].Record.Id);
    }

    [Fact]
    public async Task Search_RespectsTopK()
    {
        for (var i = 0; i < 10; i++)
        {
            await _backend.StoreAsync(CreateRecord($"r{i}", [1.0f, (float)i / 10]));
        }

        var results = await _backend.SearchAsync(new ReadOnlyMemory<float>([1.0f, 0.5f]), topK: 3);
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task Store_Upsert_OverwritesExisting()
    {
        await _backend.StoreAsync(CreateRecord("r1", [1.0f]));
        await _backend.StoreAsync(new MemoryRecord
        {
            Id = "r1",
            Text = "Updated text",
            Embedding = HalfEmbedding,
            CreatedAt = DateTimeOffset.UtcNow,
            LastAccessedAt = DateTimeOffset.UtcNow,
        });

        var result = await _backend.GetAsync("r1");
        Assert.Equal("Updated text", result!.Text);
    }
}
