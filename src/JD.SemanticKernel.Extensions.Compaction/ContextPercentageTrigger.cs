using System;
using Microsoft.SemanticKernel.ChatCompletion;

namespace JD.SemanticKernel.Extensions.Compaction;

/// <summary>
/// Triggers compaction when context usage exceeds a percentage of the model's context window.
/// </summary>
public sealed class ContextPercentageTrigger : ICompactionTrigger
{
    private readonly CompactionOptions _options;

    /// <summary>Initializes a new instance of <see cref="ContextPercentageTrigger"/>.</summary>
    public ContextPercentageTrigger(CompactionOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public bool ShouldCompact(ChatHistory history)
    {
        if (history is null || history.Count < _options.MinMessagesBeforeCompaction)
        {
            return false;
        }

        var estimatedTokens = TokenEstimator.EstimateTokens(history);
        var threshold = _options.MaxContextWindowTokens * _options.Threshold;
        return estimatedTokens > threshold;
    }
}
