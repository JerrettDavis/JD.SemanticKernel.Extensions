using System;

namespace JD.SemanticKernel.Extensions.Memory;

/// <summary>
/// Adjusts relevance scores based on memory age using exponential decay.
/// </summary>
public static class TemporalDecayScorer
{
    /// <summary>
    /// Applies temporal decay to a relevance score.
    /// </summary>
    /// <param name="baseScore">The original relevance score.</param>
    /// <param name="createdAt">When the memory was created.</param>
    /// <param name="halfLifeDays">Half-life in days (score halves every N days).</param>
    /// <param name="now">Current time. Defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
    /// <returns>The decay-adjusted score.</returns>
    public static double ApplyDecay(
        double baseScore,
        DateTimeOffset createdAt,
        double halfLifeDays,
        DateTimeOffset? now = null)
    {
        if (halfLifeDays <= 0)
        {
            return baseScore;
        }

        var currentTime = now ?? DateTimeOffset.UtcNow;
        var ageDays = (currentTime - createdAt).TotalDays;

        if (ageDays <= 0)
        {
            return baseScore;
        }

        // Exponential decay: score * 2^(-age/halfLife)
        var decayFactor = Math.Pow(2.0, -ageDays / halfLifeDays);
        return baseScore * decayFactor;
    }
}
