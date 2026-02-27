using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NSubstitute;
using Xunit;

#pragma warning disable CA1861 // Constant arrays

namespace JD.SemanticKernel.Extensions.Compaction.Tests;

public class CompactionFilterTests
{
    private readonly ICompactionTrigger _trigger = Substitute.For<ICompactionTrigger>();
    private readonly ICompactionStrategy _strategy = Substitute.For<ICompactionStrategy>();
    private readonly CompactionOptions _options = new();

    private CompactionFilter CreateFilter() => new(_trigger, _strategy, _options);

    [Fact]
    public void Constructor_NullTrigger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CompactionFilter(null!, _strategy, _options));
    }

    [Fact]
    public void Constructor_NullStrategy_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CompactionFilter(_trigger, null!, _options));
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new CompactionFilter(_trigger, _strategy, null!));
    }

    [Fact]
    public async Task OnAutoFunctionInvocationAsync_WhenShouldCompactFalse_DoesNotCallStrategy()
    {
        var filter = CreateFilter();
        var history = new ChatHistory();
        history.AddUserMessage("Hello");

        _trigger.ShouldCompact(history).Returns(false);

        var kernel = Kernel.CreateBuilder().Build();
        var function = KernelFunctionFactory.CreateFromMethod(() => "result", "TestFunc");
        var chatMessage = new ChatMessageContent(AuthorRole.Assistant, "response");
        var context = new AutoFunctionInvocationContext(kernel, function, new FunctionResult(function), history, chatMessage);

        var nextCalled = false;
        await filter.OnAutoFunctionInvocationAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await _strategy.DidNotReceive().CompactAsync(
            Arg.Any<ChatHistory>(),
            Arg.Any<Kernel>(),
            Arg.Any<CompactionOptions>(),
            Arg.Any<CancellationToken>());
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task OnAutoFunctionInvocationAsync_WhenShouldCompactTrue_CallsStrategy()
    {
        var filter = CreateFilter();
        var history = new ChatHistory();
        history.AddUserMessage("Hello");

        _trigger.ShouldCompact(history).Returns(true);

        var compactedHistory = new ChatHistory();
        compactedHistory.AddAssistantMessage("[Summary]");

        _strategy.CompactAsync(history, Arg.Any<Kernel>(), _options, Arg.Any<CancellationToken>())
            .Returns(compactedHistory);

        var kernel = Kernel.CreateBuilder().Build();
        var function = KernelFunctionFactory.CreateFromMethod(() => "result", "TestFunc");
        var chatMessage = new ChatMessageContent(AuthorRole.Assistant, "response");
        var context = new AutoFunctionInvocationContext(kernel, function, new FunctionResult(function), history, chatMessage);

        await filter.OnAutoFunctionInvocationAsync(context, _ => Task.CompletedTask);

        await _strategy.Received(1).CompactAsync(
            Arg.Any<ChatHistory>(),
            Arg.Any<Kernel>(),
            _options,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OnAutoFunctionInvocationAsync_AlwaysCallsNext()
    {
        var filter = CreateFilter();
        var history = new ChatHistory();
        history.AddUserMessage("test");
        _trigger.ShouldCompact(Arg.Any<ChatHistory>()).Returns(false);

        var kernel = Kernel.CreateBuilder().Build();
        var function = KernelFunctionFactory.CreateFromMethod(() => "result", "TestFunc");
        var chatMessage = new ChatMessageContent(AuthorRole.Assistant, "response");
        var context = new AutoFunctionInvocationContext(kernel, function, new FunctionResult(function), history, chatMessage);

        var nextCalled = false;
        await filter.OnAutoFunctionInvocationAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task OnAutoFunctionInvocationAsync_CompactReplacesHistory()
    {
        var filter = CreateFilter();
        var history = new ChatHistory();
        history.AddUserMessage("Msg1");
        history.AddAssistantMessage("Msg2");

        _trigger.ShouldCompact(history).Returns(true);

        var compacted = new ChatHistory();
        compacted.AddAssistantMessage("[Summary]");

        _strategy.CompactAsync(history, Arg.Any<Kernel>(), _options, Arg.Any<CancellationToken>())
            .Returns(compacted);

        var kernel = Kernel.CreateBuilder().Build();
        var function = KernelFunctionFactory.CreateFromMethod(() => "result", "TestFunc");
        var chatMessage = new ChatMessageContent(AuthorRole.Assistant, "response");
        var context = new AutoFunctionInvocationContext(kernel, function, new FunctionResult(function), history, chatMessage);

        await filter.OnAutoFunctionInvocationAsync(context, _ => Task.CompletedTask);

        Assert.Single(context.ChatHistory!);
        Assert.Equal("[Summary]", context.ChatHistory![0].Content);
    }
}
