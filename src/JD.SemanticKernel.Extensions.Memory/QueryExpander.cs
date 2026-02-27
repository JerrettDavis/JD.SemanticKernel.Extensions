using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace JD.SemanticKernel.Extensions.Memory;

/// <summary>
/// Expands search queries using an LLM to improve recall.
/// </summary>
public sealed class QueryExpander
{
    private const string ExpansionPrompt =
        "Given the search query below, generate 3 alternative phrasings or related " +
        "terms that would help find relevant information. Return ONLY the alternatives, " +
        "one per line, no numbering or prefixes.\n\nQuery: {0}";

    private static readonly char[] LineSeparators = { '\n', '\r' };

    /// <summary>
    /// Expands a query into multiple alternative phrasings.
    /// </summary>
    /// <param name="query">The original query.</param>
    /// <param name="kernel">The kernel for LLM access.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The original query plus expanded alternatives.</returns>
    public async Task<IReadOnlyList<string>> ExpandAsync(
        string query,
        Kernel kernel,
        CancellationToken cancellationToken = default)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(kernel);
#else
        if (kernel is null) throw new ArgumentNullException(nameof(kernel));
#endif

        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<string>();
        }

        var results = new List<string> { query };

        try
        {
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory();
#pragma warning disable CA1863 // CompositeFormat not available on netstandard2.0
            history.AddUserMessage(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                ExpansionPrompt,
                query));
#pragma warning restore CA1863

            var response = await chatService.GetChatMessageContentAsync(
                history,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                var lines = response.Content!
                    .Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.Trim())
                    .Where(l => l.Length > 0);

                results.AddRange(lines);
            }
        }
#pragma warning disable CA1031 // Do not catch general exception types — query expansion is best-effort
        catch
#pragma warning restore CA1031
        {
            // Query expansion is best-effort; return original query on failure
        }

        return results;
    }
}
