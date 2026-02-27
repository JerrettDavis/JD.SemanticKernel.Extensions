using System;
using System.Collections.Generic;

namespace JD.SemanticKernel.Extensions.Memory;

/// <summary>
/// Options for semantic memory search.
/// </summary>
public sealed class MemorySearchOptions
{
    /// <summary>Maximum number of results to return.</summary>
    public int TopK { get; set; } = 10;

    /// <summary>Minimum relevance score (0.0–1.0) to include a result.</summary>
    public double MinRelevanceScore { get; set; } = 0.5;

    /// <summary>Enable Maximal Marginal Relevance for diverse results.</summary>
    public bool UseMmr { get; set; }

    /// <summary>
    /// MMR lambda parameter (0.0 = all diversity, 1.0 = all relevance).
    /// Only used when <see cref="UseMmr"/> is <c>true</c>.
    /// </summary>
    public double MmrLambda { get; set; } = 0.7;

    /// <summary>
    /// Temporal decay half-life in days. Memories lose 50% relevance weight
    /// every N days. Set to 0 or negative to disable temporal decay.
    /// </summary>
    public double TemporalDecayHalfLifeDays { get; set; }

    /// <summary>Enable automatic query expansion for better recall.</summary>
    public bool UseQueryExpansion { get; set; }

    /// <summary>Metadata key-value filters. Only memories matching ALL filters are returned.</summary>
#pragma warning disable CA2227 // Collection properties should be read only — options DTO requires setter
    public IDictionary<string, string> Filters { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
#pragma warning restore CA2227
}
