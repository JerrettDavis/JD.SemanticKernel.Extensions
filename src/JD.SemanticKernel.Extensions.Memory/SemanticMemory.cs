using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
#pragma warning disable CS0618 // ITextEmbeddingGenerationService is obsolete but still the SK standard for netstandard2.0
using Microsoft.SemanticKernel.Embeddings;
#pragma warning restore CS0618

namespace JD.SemanticKernel.Extensions.Memory;

/// <summary>
/// Default implementation of <see cref="ISemanticMemory"/> using SK's embedding service
/// and a pluggable <see cref="IMemoryBackend"/>.
/// </summary>
public sealed class SemanticMemory : ISemanticMemory
{
    private readonly IMemoryBackend _backend;
    private readonly Kernel _kernel;
    private readonly MemorySearchOptions _defaultOptions;
    private readonly QueryExpander _queryExpander;

    /// <summary>Initializes a new instance of <see cref="SemanticMemory"/>.</summary>
    public SemanticMemory(
        IMemoryBackend backend,
        Kernel kernel,
        MemorySearchOptions? defaultOptions = null)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _defaultOptions = defaultOptions ?? new MemorySearchOptions();
        _queryExpander = new QueryExpander();
    }

    /// <inheritdoc />
    public async Task<string> StoreAsync(
        string text,
        IDictionary<string, string>? metadata = null,
        string? id = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be null or whitespace.", nameof(text));
        }

#pragma warning disable CS0618
        var embeddingService = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore CS0618
        var embedding = await embeddingService.GenerateEmbeddingAsync(
            text, cancellationToken: cancellationToken).ConfigureAwait(false);

        var recordId = id ?? Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture);

        var record = new MemoryRecord
        {
            Id = recordId,
            Text = text,
            Embedding = embedding,
            Metadata = metadata != null
                ? new Dictionary<string, string>(metadata, StringComparer.Ordinal)
                : new Dictionary<string, string>(StringComparer.Ordinal),
            CreatedAt = DateTimeOffset.UtcNow,
            LastAccessedAt = DateTimeOffset.UtcNow,
        };

        await _backend.StoreAsync(record, cancellationToken).ConfigureAwait(false);
        return recordId;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MemoryResult>> SearchAsync(
        string query,
        MemorySearchOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<MemoryResult>();
        }

        var opts = options ?? _defaultOptions;

        // Generate query embedding
#pragma warning disable CS0618
        var embeddingService = _kernel.GetRequiredService<ITextEmbeddingGenerationService>();
#pragma warning restore CS0618

        // Optionally expand the query
        IReadOnlyList<string> queries;
        if (opts.UseQueryExpansion)
        {
            queries = await _queryExpander.ExpandAsync(query, _kernel, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            queries = new[] { query };
        }

        // Search with all query variants and merge results
        var allCandidates = new Dictionary<string, (MemoryRecord Record, double Score)>(StringComparer.Ordinal);

        foreach (var q in queries)
        {
            var embedding = await embeddingService.GenerateEmbeddingAsync(
                q, cancellationToken: cancellationToken).ConfigureAwait(false);

            var fetchSize = opts.UseMmr ? opts.TopK * 3 : opts.TopK;
            var results = await _backend.SearchAsync(embedding, fetchSize, cancellationToken).ConfigureAwait(false);

            foreach (var (record, score) in results)
            {
                if (!allCandidates.TryGetValue(record.Id, out var existing) || score > existing.Score)
                {
                    allCandidates[record.Id] = (record, score);
                }
            }
        }

        var candidates = allCandidates.Values
            .Where(x => x.Score >= opts.MinRelevanceScore)
            .ToList();

        // Apply metadata filters
        if (opts.Filters.Count > 0)
        {
            candidates = candidates
                .Where(x => opts.Filters.All(f =>
                    x.Record.Metadata.TryGetValue(f.Key, out var val) &&
                    string.Equals(val, f.Value, StringComparison.Ordinal)))
                .ToList();
        }

        // Apply MMR reranking if enabled
        IReadOnlyList<(MemoryRecord Record, double Score)> ranked;
        if (opts.UseMmr)
        {
            var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(
                query, cancellationToken: cancellationToken).ConfigureAwait(false);
            ranked = MmrReranker.Rerank(candidates, queryEmbedding, opts.MmrLambda, opts.TopK);
        }
        else
        {
            ranked = candidates
                .OrderByDescending(x => x.Score)
                .Take(opts.TopK)
                .ToList();
        }

        // Build results with temporal decay
        var results2 = new List<MemoryResult>();
        foreach (var (record, score) in ranked)
        {
            var adjustedScore = opts.TemporalDecayHalfLifeDays > 0
                ? TemporalDecayScorer.ApplyDecay(score, record.CreatedAt, opts.TemporalDecayHalfLifeDays)
                : score;

            results2.Add(new MemoryResult
            {
                Record = record,
                RelevanceScore = score,
                AdjustedScore = adjustedScore,
                MmrSelected = opts.UseMmr,
            });
        }

        // Re-sort by adjusted score if temporal decay was applied
        if (opts.TemporalDecayHalfLifeDays > 0)
        {
            results2.Sort((a, b) => b.AdjustedScore.CompareTo(a.AdjustedScore));
        }

        return results2;
    }

    /// <inheritdoc />
    public Task ForgetAsync(string id, CancellationToken cancellationToken = default)
    {
        return _backend.DeleteAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return _backend.ExistsAsync(id, cancellationToken);
    }
}
