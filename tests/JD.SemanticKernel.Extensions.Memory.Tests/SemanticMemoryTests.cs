using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.DependencyInjection;
#pragma warning disable CS0618
using Microsoft.SemanticKernel.Embeddings;
#pragma warning restore CS0618
using NSubstitute;
using Xunit;

namespace JD.SemanticKernel.Extensions.Memory.Tests;

public class SemanticMemoryTests
{
    private readonly IMemoryBackend _backend = Substitute.For<IMemoryBackend>();
    private static readonly ReadOnlyMemory<float> TestEmbedding = new float[] { 1.0f, 0.0f, 0.0f };

#pragma warning disable CS0618
    private static (Kernel Kernel, ITextEmbeddingGenerationService EmbeddingService) CreateKernelWithEmbedding(
        ReadOnlyMemory<float> embedding)
    {
        var embeddingService = Substitute.For<ITextEmbeddingGenerationService>();
        // GenerateEmbeddingAsync is an extension method that calls GenerateEmbeddingsAsync
        embeddingService.GenerateEmbeddingsAsync(
                Arg.Any<IList<string>>(),
                Arg.Any<Kernel>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult<IList<ReadOnlyMemory<float>>>(
                new List<ReadOnlyMemory<float>> { embedding }));

        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(embeddingService);
        return (builder.Build(), embeddingService);
    }
#pragma warning restore CS0618

    [Fact]
    public async Task StoreAsync_GeneratesEmbeddingAndCallsBackend()
    {
        var (kernel, embeddingService) = CreateKernelWithEmbedding(TestEmbedding);
        var memory = new SemanticMemory(_backend, kernel);

        var id = await memory.StoreAsync("test text");

        Assert.NotNull(id);
#pragma warning disable CS0618
        await embeddingService.Received(1).GenerateEmbeddingsAsync(
            Arg.Any<IList<string>>(), Arg.Any<Kernel>(), Arg.Any<CancellationToken>());
#pragma warning restore CS0618
        await _backend.Received(1).StoreAsync(
            Arg.Is<MemoryRecord>(r => r.Text == "test text"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StoreAsync_WithCustomId_UsesProvidedId()
    {
        var (kernel, _) = CreateKernelWithEmbedding(TestEmbedding);
        var memory = new SemanticMemory(_backend, kernel);

        var id = await memory.StoreAsync("text", id: "custom-id");

        Assert.Equal("custom-id", id);
        await _backend.Received(1).StoreAsync(
            Arg.Is<MemoryRecord>(r => r.Id == "custom-id"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StoreAsync_WithMetadata_PassesMetadataToBackend()
    {
        var (kernel, _) = CreateKernelWithEmbedding(TestEmbedding);
        var memory = new SemanticMemory(_backend, kernel);
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal) { ["key"] = "value" };

        await memory.StoreAsync("text", metadata: metadata);

        await _backend.Received(1).StoreAsync(
            Arg.Is<MemoryRecord>(r => r.Metadata["key"] == "value"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StoreAsync_EmptyText_ThrowsArgumentException()
    {
        var (kernel, _) = CreateKernelWithEmbedding(TestEmbedding);
        var memory = new SemanticMemory(_backend, kernel);

        await Assert.ThrowsAsync<ArgumentException>(() => memory.StoreAsync(""));
    }

    [Fact]
    public async Task SearchAsync_GeneratesEmbeddingAndSearchesBackend()
    {
        var record = new MemoryRecord { Id = "r1", Text = "result", Embedding = TestEmbedding };
        _backend.SearchAsync(Arg.Any<ReadOnlyMemory<float>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<(MemoryRecord, double)> { (record, 0.9) });

        var (kernel, _) = CreateKernelWithEmbedding(TestEmbedding);
        var memory = new SemanticMemory(_backend, kernel);

        var results = await memory.SearchAsync("query");

        Assert.Single(results);
        Assert.Equal("r1", results[0].Record.Id);
        Assert.Equal(0.9, results[0].RelevanceScore);
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsEmpty()
    {
        var (kernel, _) = CreateKernelWithEmbedding(TestEmbedding);
        var memory = new SemanticMemory(_backend, kernel);

        var results = await memory.SearchAsync("");

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_WithMinRelevanceScoreFiltering()
    {
        var record1 = new MemoryRecord { Id = "high", Text = "high", Embedding = TestEmbedding };
        var record2 = new MemoryRecord { Id = "low", Text = "low", Embedding = TestEmbedding };
        _backend.SearchAsync(Arg.Any<ReadOnlyMemory<float>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<(MemoryRecord, double)> { (record1, 0.9), (record2, 0.3) });

        var (kernel, _) = CreateKernelWithEmbedding(TestEmbedding);
        var memory = new SemanticMemory(_backend, kernel);
        var options = new MemorySearchOptions { MinRelevanceScore = 0.5 };

        var results = await memory.SearchAsync("query", options);

        Assert.Single(results);
        Assert.Equal("high", results[0].Record.Id);
    }

    private static readonly float[] EmbeddingR1 = [1.0f, 0.0f, 0.0f];
    private static readonly float[] EmbeddingR2 = [0.9f, 0.1f, 0.0f];
    private static readonly float[] EmbeddingR3 = [0.0f, 1.0f, 0.0f];

    [Fact]
    public async Task SearchAsync_WithMmrReranking()
    {
        var records = new List<(MemoryRecord, double)>
        {
            (new MemoryRecord { Id = "r1", Text = "a", Embedding = EmbeddingR1 }, 0.95),
            (new MemoryRecord { Id = "r2", Text = "b", Embedding = EmbeddingR2 }, 0.90),
            (new MemoryRecord { Id = "r3", Text = "c", Embedding = EmbeddingR3 }, 0.70),
        };
        _backend.SearchAsync(Arg.Any<ReadOnlyMemory<float>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(records);

        var (kernel, _) = CreateKernelWithEmbedding(TestEmbedding);
        var memory = new SemanticMemory(_backend, kernel);
        var options = new MemorySearchOptions { UseMmr = true, TopK = 2, MinRelevanceScore = 0.0 };

        var results = await memory.SearchAsync("query", options);

        Assert.Equal(2, results.Count);
        Assert.True(results[0].MmrSelected);
    }

    [Fact]
    public async Task SearchAsync_WithTemporalDecay()
    {
        var recentRecord = new MemoryRecord
        {
            Id = "recent",
            Text = "recent",
            Embedding = TestEmbedding,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var oldRecord = new MemoryRecord
        {
            Id = "old",
            Text = "old",
            Embedding = TestEmbedding,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-60),
        };
        _backend.SearchAsync(Arg.Any<ReadOnlyMemory<float>>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<(MemoryRecord, double)> { (recentRecord, 0.8), (oldRecord, 0.85) });

        var (kernel, _) = CreateKernelWithEmbedding(TestEmbedding);
        var memory = new SemanticMemory(_backend, kernel);
        var options = new MemorySearchOptions { TemporalDecayHalfLifeDays = 30, MinRelevanceScore = 0.0 };

        var results = await memory.SearchAsync("query", options);

        Assert.Equal(2, results.Count);
        // Recent record should have higher adjusted score after decay
        Assert.Equal("recent", results[0].Record.Id);
    }

    [Fact]
    public async Task ForgetAsync_DelegatesToBackend()
    {
        var (kernel, _) = CreateKernelWithEmbedding(TestEmbedding);
        var memory = new SemanticMemory(_backend, kernel);

        await memory.ForgetAsync("id1");

        await _backend.Received(1).DeleteAsync("id1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistsAsync_DelegatesToBackend()
    {
        _backend.ExistsAsync("id1", Arg.Any<CancellationToken>()).Returns(true);
        var (kernel, _) = CreateKernelWithEmbedding(TestEmbedding);
        var memory = new SemanticMemory(_backend, kernel);

        var exists = await memory.ExistsAsync("id1");

        Assert.True(exists);
        await _backend.Received(1).ExistsAsync("id1", Arg.Any<CancellationToken>());
    }
}
