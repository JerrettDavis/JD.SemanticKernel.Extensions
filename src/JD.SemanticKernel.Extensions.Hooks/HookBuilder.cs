using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace JD.SemanticKernel.Extensions.Hooks;

/// <summary>
/// Fluent builder for configuring Claude Code hooks on a Semantic Kernel builder.
/// </summary>
public sealed class HookBuilder
{
    private readonly List<IFunctionInvocationFilter> _functionFilters = [];
    private readonly List<IPromptRenderFilter> _promptFilters = [];
    private readonly List<Action<ExtensionEvent>> _eventHandlers = [];

    /// <summary>
    /// Registers a handler that fires before a matching function is invoked.
    /// </summary>
    /// <param name="toolPattern">Regex pattern matching function names (e.g., "Bash|Execute").</param>
    /// <param name="handler">Async handler receiving the invocation context.</param>
    /// <returns>This builder for chaining.</returns>
    public HookBuilder OnFunctionInvoking(
        string toolPattern,
        Func<FunctionInvocationContext, Task> handler)
    {
        _functionFilters.Add(new SkHookFilter(
            preToolPattern: toolPattern,
            preHandler: handler));
        return this;
    }

    /// <summary>
    /// Registers a handler that fires after a matching function is invoked.
    /// </summary>
    /// <param name="toolPattern">Regex pattern matching function names.</param>
    /// <param name="handler">Async handler receiving the invocation context.</param>
    /// <returns>This builder for chaining.</returns>
    public HookBuilder OnFunctionInvoked(
        string toolPattern,
        Func<FunctionInvocationContext, Task> handler)
    {
        _functionFilters.Add(new SkHookFilter(
            postToolPattern: toolPattern,
            postHandler: handler));
        return this;
    }

    /// <summary>
    /// Registers a handler that fires before prompt rendering.
    /// </summary>
    /// <param name="handler">Async handler receiving the prompt render context.</param>
    /// <returns>This builder for chaining.</returns>
    public HookBuilder OnPromptRendering(Func<PromptRenderContext, Task> handler)
    {
        _promptFilters.Add(new SkPromptHookFilter(renderingHandler: handler));
        return this;
    }

    /// <summary>
    /// Registers a handler that fires after prompt rendering.
    /// </summary>
    /// <param name="handler">Async handler receiving the prompt render context.</param>
    /// <returns>This builder for chaining.</returns>
    public HookBuilder OnPromptRendered(Func<PromptRenderContext, Task> handler)
    {
        _promptFilters.Add(new SkPromptHookFilter(renderedHandler: handler));
        return this;
    }

    /// <summary>
    /// Registers a handler for custom extension events (SessionStart, SessionEnd, etc.).
    /// </summary>
    /// <param name="handler">Handler receiving the extension event.</param>
    /// <returns>This builder for chaining.</returns>
    public HookBuilder OnEvent(Action<ExtensionEvent> handler)
    {
        _eventHandlers.Add(handler);
        return this;
    }

    /// <summary>
    /// Gets the configured function invocation filters.
    /// </summary>
    internal IReadOnlyList<IFunctionInvocationFilter> FunctionFilters => _functionFilters.AsReadOnly();

    /// <summary>
    /// Gets the configured prompt render filters.
    /// </summary>
    internal IReadOnlyList<IPromptRenderFilter> PromptFilters => _promptFilters.AsReadOnly();

    /// <summary>
    /// Gets the configured event handlers.
    /// </summary>
    internal IReadOnlyList<Action<ExtensionEvent>> EventHandlers => _eventHandlers.AsReadOnly();
}
