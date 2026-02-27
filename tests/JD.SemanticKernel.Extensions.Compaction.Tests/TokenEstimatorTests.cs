using Microsoft.SemanticKernel.ChatCompletion;
using Xunit;

namespace JD.SemanticKernel.Extensions.Compaction.Tests;

public class TokenEstimatorTests
{
    [Fact]
    public void EstimateTokens_NullString_ReturnsZero()
    {
        Assert.Equal(0, TokenEstimator.EstimateTokens((string?)null));
    }

    [Fact]
    public void EstimateTokens_EmptyString_ReturnsZero()
    {
        Assert.Equal(0, TokenEstimator.EstimateTokens(string.Empty));
    }

    [Fact]
    public void EstimateTokens_ShortText_ReturnsExpected()
    {
        // "Hello" = 5 chars / 4.0 = 1.25, ceiling = 2
        Assert.Equal(2, TokenEstimator.EstimateTokens("Hello"));
    }

    [Fact]
    public void EstimateTokens_NullHistory_ReturnsZero()
    {
        Assert.Equal(0, TokenEstimator.EstimateTokens((ChatHistory?)null));
    }

    [Fact]
    public void EstimateTokens_EmptyHistory_ReturnsZero()
    {
        var history = new ChatHistory();
        Assert.Equal(0, TokenEstimator.EstimateTokens(history));
    }

    [Fact]
    public void EstimateTokens_HistoryWithMessages_IncludesOverhead()
    {
        var history = new ChatHistory();
        history.AddUserMessage("Hello world test");
        // "Hello world test" = 16 chars / 4.0 = 4 tokens + 4 overhead = 8
        Assert.Equal(8, TokenEstimator.EstimateTokens(history));
    }
}
