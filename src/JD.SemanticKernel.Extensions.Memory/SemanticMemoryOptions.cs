namespace JD.SemanticKernel.Extensions.Memory;

/// <summary>
/// Configuration options for semantic memory.
/// </summary>
public sealed class SemanticMemoryOptions
{
    /// <summary>Default search options applied when none are provided to <see cref="ISemanticMemory.SearchAsync"/>.</summary>
    public MemorySearchOptions DefaultSearchOptions { get; set; } = new MemorySearchOptions();
}
