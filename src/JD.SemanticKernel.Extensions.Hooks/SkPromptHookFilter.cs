using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace JD.SemanticKernel.Extensions.Hooks;

/// <summary>
/// Semantic Kernel <see cref="IPromptRenderFilter"/> that executes Claude Code hooks
/// before and after prompt rendering.
/// </summary>
public sealed class SkPromptHookFilter : IPromptRenderFilter
{
    private readonly Func<PromptRenderContext, Task>? _renderingHandler;
    private readonly Func<PromptRenderContext, Task>? _renderedHandler;

    /// <summary>
    /// Initializes a new instance of <see cref="SkPromptHookFilter"/>.
    /// </summary>
    /// <param name="renderingHandler">Handler invoked before prompt rendering.</param>
    /// <param name="renderedHandler">Handler invoked after prompt rendering.</param>
    public SkPromptHookFilter(
        Func<PromptRenderContext, Task>? renderingHandler = null,
        Func<PromptRenderContext, Task>? renderedHandler = null)
    {
        _renderingHandler = renderingHandler;
        _renderedHandler = renderedHandler;
    }

    /// <inheritdoc/>
    public async Task OnPromptRenderAsync(
        PromptRenderContext context,
        Func<PromptRenderContext, Task> next)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
#else
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (next is null) throw new ArgumentNullException(nameof(next));
#endif

        if (_renderingHandler is not null)
            await _renderingHandler(context).ConfigureAwait(false);

        await next(context).ConfigureAwait(false);

        if (_renderedHandler is not null)
            await _renderedHandler(context).ConfigureAwait(false);
    }
}
