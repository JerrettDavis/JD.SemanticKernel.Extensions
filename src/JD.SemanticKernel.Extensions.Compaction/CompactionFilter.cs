using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace JD.SemanticKernel.Extensions.Compaction;

/// <summary>
/// Semantic Kernel <see cref="IAutoFunctionInvocationFilter"/> that transparently
/// monitors and compacts chat history when trigger conditions are met.
/// </summary>
public sealed class CompactionFilter : IAutoFunctionInvocationFilter
{
    private readonly ICompactionTrigger _trigger;
    private readonly ICompactionStrategy _strategy;
    private readonly CompactionOptions _options;

    /// <summary>Initializes a new instance of <see cref="CompactionFilter"/>.</summary>
    public CompactionFilter(
        ICompactionTrigger trigger,
        ICompactionStrategy strategy,
        CompactionOptions options)
    {
        _trigger = trigger ?? throw new ArgumentNullException(nameof(trigger));
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public async Task OnAutoFunctionInvocationAsync(
        AutoFunctionInvocationContext context,
        Func<AutoFunctionInvocationContext, Task> next)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);
#else
        if (context is null) throw new ArgumentNullException(nameof(context));
        if (next is null) throw new ArgumentNullException(nameof(next));
#endif

        // Check if compaction is needed before the function call
        if (context.ChatHistory is not null && _trigger.ShouldCompact(context.ChatHistory))
        {
            var compacted = await _strategy.CompactAsync(
                context.ChatHistory,
                context.Kernel,
                _options,
                context.CancellationToken).ConfigureAwait(false);

            // Replace the chat history contents
            context.ChatHistory.Clear();
            foreach (var message in compacted)
            {
                context.ChatHistory.Add(message);
            }
        }

        await next(context).ConfigureAwait(false);
    }
}
