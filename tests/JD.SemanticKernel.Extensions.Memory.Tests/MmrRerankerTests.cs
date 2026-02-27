using System;
using System.Collections.Generic;
using Xunit;

namespace JD.SemanticKernel.Extensions.Memory.Tests;

public class MmrRerankerTests
{
    private static readonly float[] UnitX = [1.0f, 0.0f];
    private static readonly float[] SingleUnit = [1.0f];
    private static readonly float[] NearUnitX = [0.99f, 0.01f];
    private static readonly float[] UnitY = [0.0f, 1.0f];

    [Fact]
    public void Rerank_EmptyCandidates_ReturnsEmpty()
    {
        var results = MmrReranker.Rerank(
            Array.Empty<(MemoryRecord, double)>(),
            SingleUnit,
            lambda: 0.7,
            topK: 5);

        Assert.Empty(results);
    }

    [Fact]
    public void Rerank_SelectsTopK()
    {
        var candidates = new List<(MemoryRecord Record, double Score)>();
        for (var i = 0; i < 10; i++)
        {
            var embedding = new float[] { (float)i / 10, 1.0f - ((float)i / 10) };
            candidates.Add((new MemoryRecord
            {
                Id = $"r{i}",
                Text = $"Text {i}",
                Embedding = embedding,
            }, 1.0 - (i * 0.1)));
        }

        var results = MmrReranker.Rerank(candidates, UnitX, lambda: 0.7, topK: 3);
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void Rerank_PureDiversity_SpreadResults()
    {
        // Two near-identical results and one different
        var candidates = new List<(MemoryRecord Record, double Score)>
        {
            (new MemoryRecord { Id = "a", Embedding = UnitX }, 0.9),
            (new MemoryRecord { Id = "b", Embedding = NearUnitX }, 0.89), // near-duplicate of a
            (new MemoryRecord { Id = "c", Embedding = UnitY }, 0.5),    // very different
        };

        // Lambda=0 means pure diversity — should prefer c over b
        var results = MmrReranker.Rerank(candidates, UnitX, lambda: 0.0, topK: 2);
        Assert.Equal(2, results.Count);
        // First pick is highest relevance, second should favor diversity
        Assert.Contains(results, r => string.Equals(r.Record.Id, "c", StringComparison.Ordinal));
    }
}
