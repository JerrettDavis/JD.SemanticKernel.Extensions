using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JD.SemanticKernel.Extensions.Memory;

/// <summary>
/// Primary interface for semantic memory operations.
/// </summary>
public interface ISemanticMemory
{
    /// <summary>Stores text in memory with automatic embedding generation.</summary>
    Task<string> StoreAsync(
        string text,
        IDictionary<string, string>? metadata = null,
        string? id = null,
        CancellationToken cancellationToken = default);

    /// <summary>Searches memory for content related to the query.</summary>
    Task<IReadOnlyList<MemoryResult>> SearchAsync(
        string query,
        MemorySearchOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a memory by ID.</summary>
    Task ForgetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Checks if a memory with the given ID exists.</summary>
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default);
}
