using System;
using Microsoft.SemanticKernel.ChatCompletion;

namespace JD.SemanticKernel.Extensions.Compaction;

/// <summary>
/// Triggers compaction when estimated token count exceeds an absolute threshold.
/// </summary>
public sealed class TokenThresholdTrigger : ICompactionTrigger
{
    private readonly CompactionOptions _options;

    /// <summary>Initializes a new instance of <see cref="TokenThresholdTrigger"/>.</summary>
    public TokenThresholdTrigger(CompactionOptions options)
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
        return estimatedTokens > _options.Threshold;
    }
}
