using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JD.SemanticKernel.Extensions.Hooks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using NSubstitute;
using Xunit;

namespace JD.SemanticKernel.Extensions.Hooks.Tests;

public class SkPromptHookFilterTests
{
    private static Kernel CreateKernelWithMockChat()
    {
        var chatService = Substitute.For<IChatCompletionService>();
        chatService.GetChatMessageContentsAsync(
                Arg.Any<ChatHistory>(),
                Arg.Any<PromptExecutionSettings>(),
                Arg.Any<Kernel>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<ChatMessageContent> { new(AuthorRole.Assistant, "response") });

        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(chatService);
        return builder.Build();
    }

    [Fact]
    public async Task RenderingHandler_Executes()
    {
        var renderingCalled = false;
        var filter = new SkPromptHookFilter(
            renderingHandler: _ => { renderingCalled = true; return Task.CompletedTask; });

        var kernel = CreateKernelWithMockChat();
        var function = KernelFunctionFactory.CreateFromPrompt("Say hello");

        kernel.PromptRenderFilters.Add(filter);
        await kernel.InvokeAsync(function);

        Assert.True(renderingCalled);
    }

    [Fact]
    public async Task RenderedHandler_Executes()
    {
        var renderedCalled = false;
        var filter = new SkPromptHookFilter(
            renderedHandler: _ => { renderedCalled = true; return Task.CompletedTask; });

        var kernel = CreateKernelWithMockChat();
        var function = KernelFunctionFactory.CreateFromPrompt("Say hello");

        kernel.PromptRenderFilters.Add(filter);
        await kernel.InvokeAsync(function);

        Assert.True(renderedCalled);
    }

    [Fact]
    public async Task BothHandlers_Execute()
    {
        var renderingCalled = false;
        var renderedCalled = false;
        var filter = new SkPromptHookFilter(
            renderingHandler: _ => { renderingCalled = true; return Task.CompletedTask; },
            renderedHandler: _ => { renderedCalled = true; return Task.CompletedTask; });

        var kernel = CreateKernelWithMockChat();
        var function = KernelFunctionFactory.CreateFromPrompt("Say hello");

        kernel.PromptRenderFilters.Add(filter);
        await kernel.InvokeAsync(function);

        Assert.True(renderingCalled);
        Assert.True(renderedCalled);
    }

    [Fact]
    public void NoHandlers_CanBeCreated()
    {
        var filter = new SkPromptHookFilter();

        Assert.NotNull(filter);
    }
}
