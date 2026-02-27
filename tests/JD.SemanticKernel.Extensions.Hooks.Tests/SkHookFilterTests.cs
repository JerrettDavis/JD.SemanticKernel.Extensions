using System;
using System.Threading.Tasks;
using JD.SemanticKernel.Extensions.Hooks;
using Microsoft.SemanticKernel;
using Xunit;

namespace JD.SemanticKernel.Extensions.Hooks.Tests;

public class SkHookFilterTests
{
    [Fact]
    public async Task PreHandler_ExecutesOnMatchingToolName()
    {
        var preCalled = false;
        var filter = new SkHookFilter(
            preToolPattern: "Bash|Execute",
            preHandler: _ => { preCalled = true; return Task.CompletedTask; });

        var kernel = Kernel.CreateBuilder().Build();
        var function = KernelFunctionFactory.CreateFromMethod(() => "result", "Bash");

        // Register the filter and invoke through kernel
        kernel.FunctionInvocationFilters.Add(filter);
        await kernel.InvokeAsync(function);

        Assert.True(preCalled);
    }

    [Fact]
    public async Task PostHandler_ExecutesOnMatchingToolName()
    {
        var postCalled = false;
        var filter = new SkHookFilter(
            postToolPattern: "Write|Edit",
            postHandler: _ => { postCalled = true; return Task.CompletedTask; });

        var kernel = Kernel.CreateBuilder().Build();
        var function = KernelFunctionFactory.CreateFromMethod(() => "result", "Write");

        kernel.FunctionInvocationFilters.Add(filter);
        await kernel.InvokeAsync(function);

        Assert.True(postCalled);
    }

    [Fact]
    public async Task PreHandler_NotExecutedOnNonMatchingToolName()
    {
        var preCalled = false;
        var filter = new SkHookFilter(
            preToolPattern: "Bash",
            preHandler: _ => { preCalled = true; return Task.CompletedTask; });

        var kernel = Kernel.CreateBuilder().Build();
        var function = KernelFunctionFactory.CreateFromMethod(() => "result", "Write");

        kernel.FunctionInvocationFilters.Add(filter);
        await kernel.InvokeAsync(function);

        Assert.False(preCalled);
    }

    [Fact]
    public async Task PostHandler_NotExecutedOnNonMatchingToolName()
    {
        var postCalled = false;
        var filter = new SkHookFilter(
            postToolPattern: "Bash",
            postHandler: _ => { postCalled = true; return Task.CompletedTask; });

        var kernel = Kernel.CreateBuilder().Build();
        var function = KernelFunctionFactory.CreateFromMethod(() => "result", "Write");

        kernel.FunctionInvocationFilters.Add(filter);
        await kernel.InvokeAsync(function);

        Assert.False(postCalled);
    }

    [Fact]
    public async Task RegexPatternMatching_Works()
    {
        var preCalled = false;
        var filter = new SkHookFilter(
            preToolPattern: @"^(Get|Set).*Config$",
            preHandler: _ => { preCalled = true; return Task.CompletedTask; });

        var kernel = Kernel.CreateBuilder().Build();
        var function = KernelFunctionFactory.CreateFromMethod(() => "result", "GetAppConfig");

        kernel.FunctionInvocationFilters.Add(filter);
        await kernel.InvokeAsync(function);

        Assert.True(preCalled);
    }

    [Fact]
    public async Task RegexPatternMatching_CaseInsensitive()
    {
        var preCalled = false;
        var filter = new SkHookFilter(
            preToolPattern: "bash",
            preHandler: _ => { preCalled = true; return Task.CompletedTask; });

        var kernel = Kernel.CreateBuilder().Build();
        var function = KernelFunctionFactory.CreateFromMethod(() => "result", "BASH");

        kernel.FunctionInvocationFilters.Add(filter);
        await kernel.InvokeAsync(function);

        Assert.True(preCalled);
    }

    [Fact]
    public async Task NoHandlers_JustCallsNext()
    {
        var filter = new SkHookFilter();

        var kernel = Kernel.CreateBuilder().Build();
        var function = KernelFunctionFactory.CreateFromMethod(() => "result", "Anything");

        kernel.FunctionInvocationFilters.Add(filter);
        var result = await kernel.InvokeAsync(function);

        Assert.Equal("result", result.GetValue<string>());
    }

    [Fact]
    public async Task BothPreAndPostHandlers_ExecuteInOrder()
    {
        var order = new System.Collections.Generic.List<string>();
        var filter = new SkHookFilter(
            preToolPattern: "Test",
            postToolPattern: "Test",
            preHandler: _ => { order.Add("pre"); return Task.CompletedTask; },
            postHandler: _ => { order.Add("post"); return Task.CompletedTask; });

        var kernel = Kernel.CreateBuilder().Build();
        var function = KernelFunctionFactory.CreateFromMethod(() => { order.Add("exec"); return "result"; }, "Test");

        kernel.FunctionInvocationFilters.Add(filter);
        await kernel.InvokeAsync(function);

        Assert.Equal(3, order.Count);
        Assert.Equal("pre", order[0]);
        Assert.Equal("exec", order[1]);
        Assert.Equal("post", order[2]);
    }
}

