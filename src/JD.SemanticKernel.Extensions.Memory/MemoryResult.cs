using System;

namespace JD.SemanticKernel.Extensions.Memory;

/// <summary>
/// A search result from semantic memory.
/// </summary>
public sealed class MemoryResult
{
    /// <summary>The memory record.</summary>
    public MemoryRecord Record { get; set; } = null!;

    /// <summary>Raw cosine similarity score (0.0–1.0).</summary>
    public double RelevanceScore { get; set; }

    /// <summary>Score after applying temporal decay.</summary>
    public double AdjustedScore { get; set; }

    /// <summary>Whether this result was selected by MMR reranking.</summary>
    public bool MmrSelected { get; set; }
}
