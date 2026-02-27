using Microsoft.SemanticKernel.ChatCompletion;
using Xunit;

namespace JD.SemanticKernel.Extensions.Compaction.Tests;

public class TokenThresholdTriggerTests
{
    [Fact]
    public void ShouldCompact_BelowThreshold_ReturnsFalse()
    {
        var options = new CompactionOptions
        {
            TriggerMode = CompactionTriggerMode.TokenThreshold,
            Threshold = 1000,
            MinMessagesBeforeCompaction = 1,
        };
        var trigger = new TokenThresholdTrigger(options);
        var history = new ChatHistory();
        history.AddUserMessage("Short message");

        Assert.False(trigger.ShouldCompact(history));
    }

    [Fact]
    public void ShouldCompact_AboveThreshold_ReturnsTrue()
    {
        var options = new CompactionOptions
        {
            TriggerMode = CompactionTriggerMode.TokenThreshold,
            Threshold = 10,
            MinMessagesBeforeCompaction = 1,
        };
        var trigger = new TokenThresholdTrigger(options);
        var history = new ChatHistory();
        // Each message ≈ (200/4) + 4 = 54 tokens
        for (var i = 0; i < 5; i++)
        {
            history.AddUserMessage(new string('x', 200));
        }

        Assert.True(trigger.ShouldCompact(history));
    }

    [Fact]
    public void ShouldCompact_NullHistory_ReturnsFalse()
    {
        var options = new CompactionOptions { Threshold = 10 };
        var trigger = new TokenThresholdTrigger(options);

        Assert.False(trigger.ShouldCompact(null!));
    }

    [Fact]
    public void ShouldCompact_TooFewMessages_ReturnsFalse()
    {
        var options = new CompactionOptions
        {
            Threshold = 10,
            MinMessagesBeforeCompaction = 50,
        };
        var trigger = new TokenThresholdTrigger(options);
        var history = new ChatHistory();
        history.AddUserMessage(new string('x', 1000));

        Assert.False(trigger.ShouldCompact(history));
    }
}
