using System.Collections.Generic;
using Microsoft.SemanticKernel.ChatCompletion;

namespace JD.SemanticKernel.Extensions.Compaction;

/// <summary>
/// Determines whether compaction should be triggered for a given chat history.
/// </summary>
public interface ICompactionTrigger
{
    /// <summary>
    /// Evaluates whether the chat history requires compaction.
    /// </summary>
    /// <param name="history">The current chat history.</param>
    /// <returns><c>true</c> if compaction should occur; otherwise <c>false</c>.</returns>
    bool ShouldCompact(ChatHistory history);
}
