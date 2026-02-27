using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace JD.SemanticKernel.Extensions.Compaction;

/// <summary>
/// Compaction strategy that progressively summarizes older messages while
/// preserving recent context and system messages.
/// </summary>
public sealed class HierarchicalSummarizationStrategy : ICompactionStrategy
{
    private const string SummarizationPrompt =
        """
        Summarize the following conversation segment concisely. Preserve:
        - Key decisions and conclusions
        - Important code snippets or technical details
        - Action items and commitments
        - Critical context needed to understand subsequent messages

        Keep the summary to approximately {0}% of the original length.
        Use a factual, neutral tone. Format as a single cohesive summary paragraph.

        Conversation segment:
        {1}
        """;

    /// <inheritdoc />
    public async Task<ChatHistory> CompactAsync(
        ChatHistory history,
        Kernel kernel,
        CompactionOptions options,
        CancellationToken cancellationToken = default)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(kernel);
        ArgumentNullException.ThrowIfNull(options);
#else
        if (history is null) throw new ArgumentNullException(nameof(history));
        if (kernel is null) throw new ArgumentNullException(nameof(kernel));
        if (options is null) throw new ArgumentNullException(nameof(options));
#endif

        if (history.Count <= options.PreserveLastMessages)
        {
            return history;
        }

        var systemMessages = new List<ChatMessageContent>();
        var compactableMessages = new List<ChatMessageContent>();
        var preservedMessages = new List<ChatMessageContent>();

        // Separate system messages, compactable messages, and preserved recent messages
        var preserveStart = Math.Max(0, history.Count - options.PreserveLastMessages);

        for (var i = 0; i < history.Count; i++)
        {
            var message = history[i];

            if (options.PreserveSystemMessages && message.Role == AuthorRole.System)
            {
                systemMessages.Add(message);
            }
            else if (i < preserveStart)
            {
                compactableMessages.Add(message);
            }
            else
            {
                preservedMessages.Add(message);
            }
        }

        if (compactableMessages.Count == 0)
        {
            return history;
        }

        // Build conversation text for summarization
        var conversationText = new StringBuilder();
        foreach (var msg in compactableMessages)
        {
            conversationText.Append('[').Append(msg.Role).Append("]: ").AppendLine(msg.Content);
        }

        var targetPercentage = (int)(options.TargetCompressionRatio * 100);
#pragma warning disable CA1863 // CompositeFormat not available on netstandard2.0
        var prompt = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            SummarizationPrompt,
            targetPercentage,
            conversationText.ToString());
#pragma warning restore CA1863

        // Use the configured summary model or the default chat completion service
        var chatService = string.IsNullOrEmpty(options.SummaryModelId)
            ? kernel.GetRequiredService<IChatCompletionService>()
            : kernel.GetRequiredService<IChatCompletionService>(options.SummaryModelId);

        var summarizationHistory = new ChatHistory();
        summarizationHistory.AddUserMessage(prompt);

        var summaryResult = await chatService.GetChatMessageContentAsync(
            summarizationHistory,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        // Build the compacted history
        var compacted = new ChatHistory();

        // Add system messages first
        foreach (var sysMsg in systemMessages)
        {
            compacted.Add(sysMsg);
        }

        // Add the summary as an assistant message with metadata
        compacted.AddAssistantMessage(
            $"[Compacted Summary of {compactableMessages.Count} earlier messages]\n{summaryResult.Content}");

        // Add preserved recent messages
        foreach (var preserved in preservedMessages)
        {
            compacted.Add(preserved);
        }

        return compacted;
    }
}
