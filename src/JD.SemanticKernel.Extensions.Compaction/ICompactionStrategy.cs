using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace JD.SemanticKernel.Extensions.Compaction;

/// <summary>
/// Defines a strategy for compacting chat history.
/// </summary>
public interface ICompactionStrategy
{
    /// <summary>
    /// Compacts the given chat history, returning a new (shorter) history.
    /// </summary>
    /// <param name="history">The current chat history to compact.</param>
    /// <param name="kernel">The kernel, used to access chat completion for summarization.</param>
    /// <param name="options">Compaction configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A compacted chat history.</returns>
    Task<ChatHistory> CompactAsync(
        ChatHistory history,
        Kernel kernel,
        CompactionOptions options,
        CancellationToken cancellationToken = default);
}
