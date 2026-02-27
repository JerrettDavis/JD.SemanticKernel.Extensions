using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JD.SemanticKernel.Extensions.Memory;

/// <summary>
/// In-memory vector store for testing and demos. Not suitable for production use.
/// </summary>
public sealed class InMemoryBackend : IMemoryBackend
{
    private readonly ConcurrentDictionary<string, MemoryRecord> _store = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Task StoreAsync(MemoryRecord record, CancellationToken cancellationToken = default)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(record);
#else
        if (record is null) throw new ArgumentNullException(nameof(record));
#endif

        _store[record.Id] = record;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<(MemoryRecord Record, double Score)>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var results = _store.Values
            .Select(record => (Record: record, Score: CosineSimilarity(queryEmbedding, record.Embedding)))
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<(MemoryRecord, double)>>(results);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.ContainsKey(id));
    }

    /// <inheritdoc />
    public Task<MemoryRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        _store.TryGetValue(id, out var record);
        return Task.FromResult<MemoryRecord?>(record);
    }

    private static double CosineSimilarity(ReadOnlyMemory<float> a, ReadOnlyMemory<float> b)
    {
        var spanA = a.Span;
        var spanB = b.Span;

        if (spanA.Length != spanB.Length || spanA.Length == 0)
        {
            return 0.0;
        }

        double dot = 0, magA = 0, magB = 0;
        for (var i = 0; i < spanA.Length; i++)
        {
            dot += spanA[i] * spanB[i];
            magA += spanA[i] * spanA[i];
            magB += spanB[i] * spanB[i];
        }

        var magnitude = Math.Sqrt(magA) * Math.Sqrt(magB);
        return magnitude > 0 ? dot / magnitude : 0.0;
    }
}
