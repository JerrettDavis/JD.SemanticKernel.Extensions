using JD.SemanticKernel.Extensions.Hooks;
using Microsoft.SemanticKernel;

namespace JD.SemanticKernel.Extensions.Hooks.Tests;

public class HookBuilderTests
{
    [Fact]
    public void OnFunctionInvoking_AddsFunctionFilter()
    {
        var hookBuilder = new HookBuilder();

        hookBuilder.OnFunctionInvoking("Bash", _ => Task.CompletedTask);

        Assert.Single(hookBuilder.FunctionFilters);
    }

    [Fact]
    public void OnFunctionInvoked_AddsFunctionFilter()
    {
        var hookBuilder = new HookBuilder();

        hookBuilder.OnFunctionInvoked("Write", _ => Task.CompletedTask);

        Assert.Single(hookBuilder.FunctionFilters);
    }

    [Fact]
    public void OnPromptRendering_AddsPromptFilter()
    {
        var hookBuilder = new HookBuilder();

        hookBuilder.OnPromptRendering(_ => Task.CompletedTask);

        Assert.Single(hookBuilder.PromptFilters);
    }

    [Fact]
    public void OnPromptRendered_AddsPromptFilter()
    {
        var hookBuilder = new HookBuilder();

        hookBuilder.OnPromptRendered(_ => Task.CompletedTask);

        Assert.Single(hookBuilder.PromptFilters);
    }

    [Fact]
    public void OnEvent_AddsEventHandler()
    {
        var hookBuilder = new HookBuilder();

        hookBuilder.OnEvent(_ => { });

        Assert.Single(hookBuilder.EventHandlers);
    }

    [Fact]
    public void Chaining_AllowsMultipleRegistrations()
    {
        var hookBuilder = new HookBuilder();

        hookBuilder
            .OnFunctionInvoking("Bash", _ => Task.CompletedTask)
            .OnFunctionInvoked("Write", _ => Task.CompletedTask)
            .OnPromptRendering(_ => Task.CompletedTask)
            .OnEvent(_ => { });

        Assert.Equal(2, hookBuilder.FunctionFilters.Count);
        Assert.Single(hookBuilder.PromptFilters);
        Assert.Single(hookBuilder.EventHandlers);
    }

    [Fact]
    public void UseHooks_RegistersFiltersInKernel()
    {
        var builder = Kernel.CreateBuilder()
            .UseHooks(hooks =>
            {
                hooks.OnFunctionInvoking(".*", _ => Task.CompletedTask);
                hooks.OnPromptRendering(_ => Task.CompletedTask);
            });

        // Should not throw and builder should be returned
        Assert.NotNull(builder);
    }
}
