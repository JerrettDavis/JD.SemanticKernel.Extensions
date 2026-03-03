using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NSubstitute;
using Xunit;

namespace JD.SemanticKernel.Extensions.Compaction.Tests;

public class HierarchicalSummarizationStrategyTests
{
    private static CompactionOptions CreateOptions(int preserveLast = 5, int minMessages = 3) => new()
    {
        PreserveLastMessages = preserveLast,
        MinMessagesBeforeCompaction = minMessages,
        PreserveSystemMessages = true,
        TargetCompressionRatio = 0.25,
    };

    private static (Kernel Kernel, IChatCompletionService ChatService) CreateKernelWithMockChat(string summaryText = "Summary of conversation.")
    {
        var chatService = Substitute.For<IChatCompletionService>();
        // GetChatMessageContentAsync is an extension; mock the actual interface method
        chatService.GetChatMessageContentsAsync(
                Arg.Any<ChatHistory>(),
                Arg.Any<PromptExecutionSettings>(),
                Arg.Any<Kernel>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<ChatMessageContent> { new(AuthorRole.Assistant, summaryText) });

        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(chatService);
        return (builder.Build(), chatService);
    }

    [Fact]
    public async Task CompactAsync_HistoryShorterThanPreserveLastMessages_ReturnsUnchanged()
    {
        var strategy = new HierarchicalSummarizationStrategy();
        var options = CreateOptions(preserveLast: 10);
        var history = new ChatHistory();
        history.AddUserMessage("Hello");
        history.AddAssistantMessage("Hi");

        var (kernel, _) = CreateKernelWithMockChat();
        var result = await strategy.CompactAsync(history, kernel, options, CancellationToken.None);

        Assert.Same(history, result);
    }

    [Fact]
    public async Task CompactAsync_NoCompactableMessages_ReturnsUnchanged()
    {
        var strategy = new HierarchicalSummarizationStrategy();
        var options = CreateOptions(preserveLast: 5);
        var history = new ChatHistory();

        // All messages are within the preserve window
        for (var i = 0; i < 5; i++)
        {
            history.AddUserMessage($"Message {i}");
        }

        var (kernel, _) = CreateKernelWithMockChat();
        var result = await strategy.CompactAsync(history, kernel, options, CancellationToken.None);

        Assert.Same(history, result);
    }

    [Fact]
    public async Task CompactAsync_SystemMessagesArePreserved()
    {
        var strategy = new HierarchicalSummarizationStrategy();
        var options = CreateOptions(preserveLast: 2);
        var history = new ChatHistory();
        history.AddSystemMessage("You are a helpful assistant.");
        // Add enough compactable messages
        for (var i = 0; i < 5; i++)
        {
            history.AddUserMessage($"Old message {i}");
        }
        history.AddUserMessage("Recent 1");
        history.AddAssistantMessage("Recent 2");

        var (kernel, _) = CreateKernelWithMockChat("Summarized.");
        var result = await strategy.CompactAsync(history, kernel, options, CancellationToken.None);

        Assert.Equal(AuthorRole.System, result[0].Role);
        Assert.Equal("You are a helpful assistant.", result[0].Content);
    }

    [Fact]
    public async Task CompactAsync_PreservesLastNMessagesVerbatim()
    {
        var strategy = new HierarchicalSummarizationStrategy();
        var options = CreateOptions(preserveLast: 2);
        var history = new ChatHistory();
        for (var i = 0; i < 5; i++)
        {
            history.AddUserMessage($"Old message {i}");
        }
        history.AddUserMessage("Keep me 1");
        history.AddAssistantMessage("Keep me 2");

        var (kernel, _) = CreateKernelWithMockChat("Summarized.");
        var result = await strategy.CompactAsync(history, kernel, options, CancellationToken.None);

        // Result: summary + 2 preserved
        Assert.Equal(3, result.Count);
        // Summary message
        Assert.Contains("Compacted Summary", result[0].Content);
        // Preserved messages at the end
        Assert.Equal("Keep me 1", result[1].Content);
        Assert.Equal("Keep me 2", result[2].Content);
    }

    [Fact]
    public async Task CompactAsync_CallsChatCompletionServiceForSummary()
    {
        var strategy = new HierarchicalSummarizationStrategy();
        var options = CreateOptions(preserveLast: 1);
        var history = new ChatHistory();
        for (var i = 0; i < 5; i++)
        {
            history.AddUserMessage($"Message {i}");
        }

        var (kernel, chatService) = CreateKernelWithMockChat("My summary.");
        var result = await strategy.CompactAsync(history, kernel, options, CancellationToken.None);

        await chatService.Received(1).GetChatMessageContentsAsync(
            Arg.Any<ChatHistory>(),
            Arg.Any<PromptExecutionSettings>(),
            Arg.Any<Kernel>(),
            Arg.Any<CancellationToken>());

        Assert.Contains("My summary.", result[0].Content);
    }

    [Fact]
    public async Task CompactAsync_NullHistory_ThrowsArgumentNullException()
    {
        var strategy = new HierarchicalSummarizationStrategy();
        var options = CreateOptions();
        var (kernel, _) = CreateKernelWithMockChat();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => strategy.CompactAsync(null!, kernel, options, CancellationToken.None));
    }

    [Fact]
    public async Task CompactAsync_NullKernel_ThrowsArgumentNullException()
    {
        var strategy = new HierarchicalSummarizationStrategy();
        var options = CreateOptions();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => strategy.CompactAsync(new ChatHistory(), null!, options, CancellationToken.None));
    }

    [Fact]
    public async Task CompactAsync_NullOptions_ThrowsArgumentNullException()
    {
        var strategy = new HierarchicalSummarizationStrategy();
        var (kernel, _) = CreateKernelWithMockChat();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => strategy.CompactAsync(new ChatHistory(), kernel, null!, CancellationToken.None));
    }
}
