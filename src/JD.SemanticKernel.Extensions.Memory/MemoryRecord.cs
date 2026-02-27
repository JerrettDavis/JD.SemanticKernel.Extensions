using System;
using System.Collections.Generic;

namespace JD.SemanticKernel.Extensions.Memory;

/// <summary>
/// Represents a stored memory record with its embedding and metadata.
/// </summary>
public sealed class MemoryRecord
{
    /// <summary>Unique identifier for this memory.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The original text content.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>The embedding vector.</summary>
    public ReadOnlyMemory<float> Embedding { get; set; }

    /// <summary>Metadata key-value pairs.</summary>
#pragma warning disable CA2227 // Collection properties should be read only — DTO requires setter
    public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
#pragma warning restore CA2227

    /// <summary>When this memory was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>When this memory was last accessed.</summary>
    public DateTimeOffset LastAccessedAt { get; set; } = DateTimeOffset.UtcNow;
}
