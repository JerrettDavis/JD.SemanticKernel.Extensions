using System;
using JD.SemanticKernel.Extensions.Mcp;

namespace JD.SemanticKernel.Extensions.Mcp.Tests;

public class McpInvocationResultTests
{
    [Fact]
    public void Success_WithContent_SetsContentAndIsErrorFalse()
    {
        var result = McpInvocationResult.Success("hello");

        Assert.Equal("hello", result.Content);
        Assert.False(result.IsError);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Success_NullContent_IsNotError()
    {
        var result = McpInvocationResult.Success(null);

        Assert.Null(result.Content);
        Assert.False(result.IsError);
    }

    [Fact]
    public void Failure_SetsIsErrorAndMessage()
    {
        var result = McpInvocationResult.Failure("tool not found");

        Assert.True(result.IsError);
        Assert.Equal("tool not found", result.ErrorMessage);
        Assert.Null(result.Content);
    }
}
