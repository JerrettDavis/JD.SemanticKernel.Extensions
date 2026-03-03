using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace JD.SemanticKernel.Extensions.Memory.Tests;

public class QueryExpanderTests
{
    [Fact]
    public async Task ExpandAsync_WithNoChatService_ReturnsOriginalQuery()
    {
        var expander = new QueryExpander();
        var kernel = Kernel.CreateBuilder().Build();

        // No IChatCompletionService registered — should catch and return original
        var results = await expander.ExpandAsync("test query", kernel, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("test query", results[0]);
    }

    [Fact]
    public async Task ExpandAsync_WithMockedChatService_ReturnsExpandedQueries()
    {
        var expander = new QueryExpander();
        var chatService = Substitute.For<IChatCompletionService>();
        chatService.GetChatMessageContentsAsync(
                Arg.Any<ChatHistory>(),
                Arg.Any<PromptExecutionSettings>(),
                Arg.Any<Kernel>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<ChatMessageContent> { new(AuthorRole.Assistant, "alternative one\nalternative two\nalternative three") });

        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(chatService);
        var kernel = builder.Build();

        var results = await expander.ExpandAsync("original query", kernel, CancellationToken.None);

        Assert.Equal(4, results.Count); // original + 3 alternatives
        Assert.Equal("original query", results[0]);
        Assert.Equal("alternative one", results[1]);
        Assert.Equal("alternative two", results[2]);
        Assert.Equal("alternative three", results[3]);
    }

    [Fact]
    public async Task ExpandAsync_OnError_FallsBackToOriginal()
    {
        var expander = new QueryExpander();
        var chatService = Substitute.For<IChatCompletionService>();
        chatService.GetChatMessageContentsAsync(
                Arg.Any<ChatHistory>(),
                Arg.Any<PromptExecutionSettings>(),
                Arg.Any<Kernel>(),
                Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Service error"));

        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(chatService);
        var kernel = builder.Build();

        var results = await expander.ExpandAsync("my query", kernel, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("my query", results[0]);
    }

    [Fact]
    public async Task ExpandAsync_EmptyQuery_ReturnsEmpty()
    {
        var expander = new QueryExpander();
        var kernel = Kernel.CreateBuilder().Build();

        var results = await expander.ExpandAsync("", kernel, CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ExpandAsync_WhitespaceQuery_ReturnsEmpty()
    {
        var expander = new QueryExpander();
        var kernel = Kernel.CreateBuilder().Build();

        var results = await expander.ExpandAsync("   ", kernel, CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ExpandAsync_NullKernel_ThrowsArgumentNullException()
    {
        var expander = new QueryExpander();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => expander.ExpandAsync("query", null!, CancellationToken.None));
    }

    [Fact]
    public async Task ExpandAsync_EmptyResponse_ReturnsOnlyOriginal()
    {
        var expander = new QueryExpander();
        var chatService = Substitute.For<IChatCompletionService>();
        chatService.GetChatMessageContentsAsync(
                Arg.Any<ChatHistory>(),
                Arg.Any<PromptExecutionSettings>(),
                Arg.Any<Kernel>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<ChatMessageContent> { new(AuthorRole.Assistant, "") });

        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(chatService);
        var kernel = builder.Build();

        var results = await expander.ExpandAsync("my query", kernel, CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("my query", results[0]);
    }
}
