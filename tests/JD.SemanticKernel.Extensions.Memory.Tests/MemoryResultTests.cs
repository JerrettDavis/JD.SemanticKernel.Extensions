using Xunit;

namespace JD.SemanticKernel.Extensions.Memory.Tests;

public class MemoryResultTests
{
    [Fact]
    public void ConstructionAndPropertyAccess()
    {
        var record = new MemoryRecord { Id = "r1", Text = "text" };

        var result = new MemoryResult
        {
            Record = record,
            RelevanceScore = 0.85,
            AdjustedScore = 0.80,
            MmrSelected = true,
        };

        Assert.Same(record, result.Record);
        Assert.Equal(0.85, result.RelevanceScore);
        Assert.Equal(0.80, result.AdjustedScore);
        Assert.True(result.MmrSelected);
    }

    [Fact]
    public void DefaultValues()
    {
        var result = new MemoryResult();

        Assert.Equal(0.0, result.RelevanceScore);
        Assert.Equal(0.0, result.AdjustedScore);
        Assert.False(result.MmrSelected);
    }
}
