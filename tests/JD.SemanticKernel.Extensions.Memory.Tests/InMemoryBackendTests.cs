using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace JD.SemanticKernel.Extensions.Memory.Tests;

public class InMemoryBackendTests
{
    private static MemoryRecord CreateRecord(string id, float[] embedding) => new()
    {
        Id = id,
        Text = $"Text for {id}",
        Embedding = embedding,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public async Task StoreAndGet_RoundTrips()
    {
        var backend = new InMemoryBackend();
        var record = CreateRecord("r1", [1.0f, 0.0f, 0.0f]);

        await backend.StoreAsync(record);
        var result = await backend.GetAsync("r1");

        Assert.NotNull(result);
        Assert.Equal("r1", result!.Id);
        Assert.Equal("Text for r1", result.Text);
    }

    [Fact]
    public async Task Exists_ReturnsTrueForStored()
    {
        var backend = new InMemoryBackend();
        await backend.StoreAsync(CreateRecord("r1", [1.0f]));

        Assert.True(await backend.ExistsAsync("r1"));
        Assert.False(await backend.ExistsAsync("nonexistent"));
    }

    [Fact]
    public async Task Delete_RemovesRecord()
    {
        var backend = new InMemoryBackend();
        await backend.StoreAsync(CreateRecord("r1", [1.0f]));
        await backend.DeleteAsync("r1");

        Assert.False(await backend.ExistsAsync("r1"));
    }

    [Fact]
    public async Task Search_ReturnsSortedByScore()
    {
        var backend = new InMemoryBackend();
        await backend.StoreAsync(CreateRecord("close", [1.0f, 0.0f, 0.0f]));
        await backend.StoreAsync(CreateRecord("far", [0.0f, 1.0f, 0.0f]));
        await backend.StoreAsync(CreateRecord("medium", [0.7f, 0.7f, 0.0f]));

        var results = await backend.SearchAsync(new ReadOnlyMemory<float>([1.0f, 0.0f, 0.0f]), topK: 3);

        Assert.Equal(3, results.Count);
        Assert.Equal("close", results[0].Record.Id);
    }

    [Fact]
    public async Task Search_RespectsTopK()
    {
        var backend = new InMemoryBackend();
        for (var i = 0; i < 10; i++)
        {
            await backend.StoreAsync(CreateRecord($"r{i}", [1.0f, (float)i / 10]));
        }

        var results = await backend.SearchAsync(new ReadOnlyMemory<float>([1.0f, 0.5f]), topK: 3);
        Assert.Equal(3, results.Count);
    }
}
