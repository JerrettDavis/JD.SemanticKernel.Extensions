using Microsoft.SemanticKernel.ChatCompletion;
using Xunit;

namespace JD.SemanticKernel.Extensions.Compaction.Tests;

public class ContextPercentageTriggerTests
{
    [Fact]
    public void ShouldCompact_Below70Percent_ReturnsFalse()
    {
        var options = new CompactionOptions
        {
            TriggerMode = CompactionTriggerMode.ContextPercentage,
            Threshold = 0.70,
            MaxContextWindowTokens = 1000,
            MinMessagesBeforeCompaction = 1,
        };
        var trigger = new ContextPercentageTrigger(options);
        var history = new ChatHistory();
        history.AddUserMessage("Short message");

        Assert.False(trigger.ShouldCompact(history));
    }

    [Fact]
    public void ShouldCompact_Above70Percent_ReturnsTrue()
    {
        var options = new CompactionOptions
        {
            TriggerMode = CompactionTriggerMode.ContextPercentage,
            Threshold = 0.70,
            MaxContextWindowTokens = 100, // small window
            MinMessagesBeforeCompaction = 1,
        };
        var trigger = new ContextPercentageTrigger(options);
        var history = new ChatHistory();
        // Fill with enough text to exceed 70 tokens (70% of 100)
        for (var i = 0; i < 10; i++)
        {
            history.AddUserMessage(new string('a', 100));
        }

        Assert.True(trigger.ShouldCompact(history));
    }
}
