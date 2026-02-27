using System;

namespace JD.SemanticKernel.Extensions.Compaction;

/// <summary>
/// Configuration options for conversation compaction.
/// </summary>
public sealed class CompactionOptions
{
    /// <summary>Determines when compaction is triggered.</summary>
    public CompactionTriggerMode TriggerMode { get; set; } = CompactionTriggerMode.ContextPercentage;

    /// <summary>
    /// Threshold value. Interpretation depends on <see cref="TriggerMode"/>:
    /// <list type="bullet">
    ///   <item><see cref="CompactionTriggerMode.ContextPercentage"/>: value between 0.0 and 1.0 (e.g., 0.70 = 70%)</item>
    ///   <item><see cref="CompactionTriggerMode.TokenThreshold"/>: absolute token count (e.g., 100_000)</item>
    /// </list>
    /// </summary>
    public double Threshold { get; set; } = 0.70;

    /// <summary>Number of most-recent messages to always keep verbatim (never compacted).</summary>
    public int PreserveLastMessages { get; set; } = 10;

    /// <summary>Whether to always preserve system messages during compaction.</summary>
    public bool PreserveSystemMessages { get; set; } = true;

    /// <summary>
    /// Optional model ID to use for generating summaries. If null, uses the default
    /// chat completion service registered in the kernel.
    /// </summary>
    public string? SummaryModelId { get; set; }

    /// <summary>
    /// Maximum context window size in tokens. Used when <see cref="TriggerMode"/> is
    /// <see cref="CompactionTriggerMode.ContextPercentage"/>.
    /// Defaults to 128,000 tokens.
    /// </summary>
    public int MaxContextWindowTokens { get; set; } = 128_000;

    /// <summary>Target compression ratio for summaries (0.0–1.0). Default 0.25 = reduce to 25% of original size.</summary>
    public double TargetCompressionRatio { get; set; } = 0.25;

    /// <summary>Minimum number of messages before compaction is even considered.</summary>
    public int MinMessagesBeforeCompaction { get; set; } = 20;
}
