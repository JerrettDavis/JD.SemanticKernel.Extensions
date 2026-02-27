using Xunit;

namespace JD.SemanticKernel.Extensions.Compaction.Tests;

public class CompactionOptionsTests
{
    [Fact]
    public void Defaults_AreReasonable()
    {
        var options = new CompactionOptions();

        Assert.Equal(CompactionTriggerMode.ContextPercentage, options.TriggerMode);
        Assert.Equal(0.70, options.Threshold);
        Assert.Equal(10, options.PreserveLastMessages);
        Assert.True(options.PreserveSystemMessages);
        Assert.Null(options.SummaryModelId);
        Assert.Equal(128_000, options.MaxContextWindowTokens);
        Assert.Equal(0.25, options.TargetCompressionRatio);
        Assert.Equal(20, options.MinMessagesBeforeCompaction);
    }
}
