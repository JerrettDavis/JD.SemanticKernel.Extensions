namespace JD.SemanticKernel.Extensions.Compaction;

/// <summary>
/// Determines when compaction should be triggered.
/// </summary>
public enum CompactionTriggerMode
{
    /// <summary>Trigger when estimated token count exceeds an absolute threshold.</summary>
    TokenThreshold,

    /// <summary>Trigger when context usage exceeds a percentage of the model's context window.</summary>
    ContextPercentage
}
