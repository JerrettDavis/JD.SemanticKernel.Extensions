using Xunit;

namespace JD.SemanticKernel.Extensions.Memory.Tests;

public class MemorySearchOptionsTests
{
    [Fact]
    public void Defaults_AreReasonable()
    {
        var options = new MemorySearchOptions();

        Assert.Equal(10, options.TopK);
        Assert.Equal(0.5, options.MinRelevanceScore);
        Assert.False(options.UseMmr);
        Assert.Equal(0.7, options.MmrLambda);
        Assert.Equal(0.0, options.TemporalDecayHalfLifeDays);
        Assert.False(options.UseQueryExpansion);
        Assert.Empty(options.Filters);
    }
}
