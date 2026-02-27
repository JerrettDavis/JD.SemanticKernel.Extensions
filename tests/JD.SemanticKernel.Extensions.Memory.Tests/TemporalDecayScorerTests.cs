using System;
using Xunit;

namespace JD.SemanticKernel.Extensions.Memory.Tests;

public class TemporalDecayScorerTests
{
    [Fact]
    public void ApplyDecay_ZeroHalfLife_ReturnsBaseScore()
    {
        var score = TemporalDecayScorer.ApplyDecay(0.9, DateTimeOffset.UtcNow.AddDays(-30), halfLifeDays: 0);
        Assert.Equal(0.9, score);
    }

    [Fact]
    public void ApplyDecay_NegativeHalfLife_ReturnsBaseScore()
    {
        var score = TemporalDecayScorer.ApplyDecay(0.9, DateTimeOffset.UtcNow.AddDays(-30), halfLifeDays: -1);
        Assert.Equal(0.9, score);
    }

    [Fact]
    public void ApplyDecay_ExactlyOneHalfLife_ReturnsHalf()
    {
        var now = DateTimeOffset.UtcNow;
        var createdAt = now.AddDays(-7); // 7 days ago
        var score = TemporalDecayScorer.ApplyDecay(1.0, createdAt, halfLifeDays: 7, now: now);

        Assert.Equal(0.5, score, precision: 5);
    }

    [Fact]
    public void ApplyDecay_TwoHalfLives_ReturnsQuarter()
    {
        var now = DateTimeOffset.UtcNow;
        var createdAt = now.AddDays(-14); // 14 days = 2x half-life of 7
        var score = TemporalDecayScorer.ApplyDecay(1.0, createdAt, halfLifeDays: 7, now: now);

        Assert.Equal(0.25, score, precision: 5);
    }

    [Fact]
    public void ApplyDecay_FutureDate_ReturnsBaseScore()
    {
        var now = DateTimeOffset.UtcNow;
        var createdAt = now.AddDays(1); // future
        var score = TemporalDecayScorer.ApplyDecay(0.8, createdAt, halfLifeDays: 7, now: now);

        Assert.Equal(0.8, score);
    }
}
