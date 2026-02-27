using System;
using Microsoft.SemanticKernel.ChatCompletion;

namespace JD.SemanticKernel.Extensions.Compaction;

/// <summary>
/// Simple token count estimator. Uses character-based heuristic (≈4 chars per token for English).
/// </summary>
public static class TokenEstimator
{
    private const double CharsPerToken = 4.0;

    /// <summary>Estimates the token count for a string.</summary>
    public static int EstimateTokens(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return (int)Math.Ceiling(text!.Length / CharsPerToken);
    }

    /// <summary>Estimates the total token count for a chat history.</summary>
    public static int EstimateTokens(ChatHistory? history)
    {
        if (history is null)
        {
            return 0;
        }

        var total = 0;
        foreach (var message in history)
        {
            total += EstimateTokens(message.Content);
            // Add overhead for role prefix (~4 tokens per message)
            total += 4;
        }

        return total;
    }
}
