using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JD.SemanticKernel.Extensions.Memory;

/// <summary>
/// Abstraction over vector storage backends.
/// </summary>
public interface IMemoryBackend
{
    /// <summary>Stores a memory record with its embedding.</summary>
    Task StoreAsync(MemoryRecord record, CancellationToken cancellationToken = default);

    /// <summary>Searches for similar memories by embedding vector.</summary>
    Task<IReadOnlyList<(MemoryRecord Record, double Score)>> SearchAsync(
        ReadOnlyMemory<float> queryEmbedding,
        int topK,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a memory by ID.</summary>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Checks if a memory with the given ID exists.</summary>
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Gets a memory by ID.</summary>
    Task<MemoryRecord?> GetAsync(string id, CancellationToken cancellationToken = default);
}
