using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace JD.SemanticKernel.Extensions.Hooks;

/// <summary>
/// Semantic Kernel <see cref="IFunctionInvocationFilter"/> that executes Claude Code hooks
/// before and after function invocations based on tool name pattern matching.
/// </summary>
public sealed class SkHookFilter : IFunctionInvocationFilter
{
    private readonly Regex? _preToolPattern;
    private readonly Regex? _postToolPattern;
    private readonly Func<FunctionInvocationContext, Task>? _preHandler;
    private readonly Func<FunctionInvocationContext, Task>? _postHandler;

    /// <summary>
    /// Initializes a new instance of <see cref="SkHookFilter"/>.
    /// </summary>
    /// <param name="preToolPattern">Regex pattern for pre-invocation matching (e.g., "Bash|Execute").</param>
    /// <param name="postToolPattern">Regex pattern for post-invocation matching.</param>
    /// <param name="preHandler">Handler invoked before matching functions.</param>
    /// <param name="postHandler">Handler invoked after matching functions.</param>
    public SkHookFilter(
        string? preToolPattern = null,
        string? postToolPattern = null,
        Func<FunctionInvocationContext, Task>? preHandler = null,
        Func<FunctionInvocationContext, Task>? postHandler = null)
    {
        _preToolPattern = preToolPattern is not null
            ? new Regex(preToolPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase)
            : null;
        _postToolPattern = postToolPattern is not null
            ? new Regex(postToolPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase)
            : null;
        _preHandler = preHandler;
        _postHandler = postHandler;
    }

    /// <inheritdoc/>
    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, Task> next)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
#else
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (next is null) throw new ArgumentNullException(nameof(next));
#endif

        var functionName = context.Function.Name;

        // Pre-invocation hook
        if (_preHandler is not null && _preToolPattern?.IsMatch(functionName) == true)
            await _preHandler(context).ConfigureAwait(false);

        await next(context).ConfigureAwait(false);

        // Post-invocation hook
        if (_postHandler is not null && _postToolPattern?.IsMatch(functionName) == true)
            await _postHandler(context).ConfigureAwait(false);
    }
}
