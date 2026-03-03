using JD.SemanticKernel.Extensions.Compaction;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Xunit;

namespace JD.SemanticKernel.Extensions.IntegrationTests;

/// <summary>
/// Integration tests for the Compaction pipeline using a real LLM (Ollama).
/// </summary>
[Trait("Category", "Integration")]
public sealed class CompactionIntegrationTests
{
    [SkippableFact]
    public void TokenEstimator_EstimatesReasonableTokenCount()
    {
        IntegrationGuard.EnsureEnabled();

        const string Text = "The quick brown fox jumps over the lazy dog.";
        var tokens = TokenEstimator.EstimateTokens(Text);

        Assert.InRange(tokens, 5, 20);
    }

    [SkippableFact]
    public void TokenThresholdTrigger_TriggersOnLargeHistory()
    {
        IntegrationGuard.EnsureEnabled();

        var options = new CompactionOptions
        {
            TriggerMode = CompactionTriggerMode.TokenThreshold,
            Threshold = 100,
            MinMessagesBeforeCompaction = 2
        };

        var trigger = new TokenThresholdTrigger(options);
        var history = new ChatHistory();
        history.AddSystemMessage("You are a helpful assistant.");

        // Accumulate enough tokens to exceed threshold
        for (int i = 0; i < 30; i++)
            history.AddUserMessage($"This is message number {i} with enough text to accumulate tokens.");

        Assert.True(trigger.ShouldCompact(history));
    }

    [SkippableFact]
    public void ContextPercentageTrigger_TriggersAtPercentage()
    {
        IntegrationGuard.EnsureEnabled();

        var options = new CompactionOptions
        {
            TriggerMode = CompactionTriggerMode.ContextPercentage,
            Threshold = 0.10, // 10% of context window
            MaxContextWindowTokens = 500,
            MinMessagesBeforeCompaction = 2
        };

        var trigger = new ContextPercentageTrigger(options);
        var history = new ChatHistory();

        for (int i = 0; i < 20; i++)
            history.AddUserMessage($"Message {i}: This is a moderately long message for testing purposes.");

        Assert.True(trigger.ShouldCompact(history));
    }

    [SkippableFact]
    public async Task HierarchicalSummarization_CompactsHistory_WithOllama()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        var kernel = OllamaConfig.CreateChatKernel();
        var strategy = new HierarchicalSummarizationStrategy();
        var options = new CompactionOptions
        {
            PreserveLastMessages = 2,
            PreserveSystemMessages = true,
            TargetCompressionRatio = 0.30
        };

        var history = new ChatHistory();
        history.AddSystemMessage("You are a coding assistant.");
        history.AddUserMessage("What is a binary search tree?");
        history.AddAssistantMessage("A binary search tree is a data structure where each node has at most two children, and for any node, the left child contains a smaller value and the right child contains a larger value.");
        history.AddUserMessage("How do you insert into a BST?");
        history.AddAssistantMessage("To insert into a BST, start at the root. If the value is less than the current node, go left; if greater, go right. Repeat until you find an empty spot and insert the node there.");
        history.AddUserMessage("What is the time complexity?");
        history.AddAssistantMessage("Average case O(log n), worst case O(n) for a degenerate tree. Balanced BSTs like AVL or Red-Black trees guarantee O(log n).");
        history.AddUserMessage("Thanks, now explain hash maps.");
        history.AddAssistantMessage("A hash map stores key-value pairs using a hash function to compute indices into an array of buckets. It provides O(1) average lookup, insert, and delete.");

        var compacted = await strategy.CompactAsync(history, kernel, options);

        // Should preserve system message
        Assert.Contains(compacted, m =>
            m.Role == AuthorRole.System &&
            m.Content != null &&
            m.Content.Contains("coding", StringComparison.OrdinalIgnoreCase));

        // Should have fewer messages than original
        Assert.True(compacted.Count < history.Count,
            $"Expected fewer messages after compaction. Original: {history.Count}, Compacted: {compacted.Count}");

        // Should preserve last messages
        Assert.True(compacted.Count >= 2, "Should preserve at least the last 2 messages");
    }

    [SkippableFact]
    public async Task HierarchicalSummarization_PreservesSystemMessage()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        var kernel = OllamaConfig.CreateChatKernel();
        var strategy = new HierarchicalSummarizationStrategy();
        var options = new CompactionOptions
        {
            PreserveLastMessages = 1,
            PreserveSystemMessages = true,
            TargetCompressionRatio = 0.25
        };

        var history = new ChatHistory();
        history.AddSystemMessage("SYSTEM: You are a financial advisor. Always include disclaimers.");

        for (int i = 0; i < 10; i++)
        {
            history.AddUserMessage($"Question {i}: Tell me about investment option {i}.");
            history.AddAssistantMessage($"Answer {i}: Investment option {i} involves various risks and returns. This is general information, not financial advice.");
        }

        var compacted = await strategy.CompactAsync(history, kernel, options);

        // System message must be preserved
        var systemMsg = compacted.FirstOrDefault(m => m.Role == AuthorRole.System);
        Assert.NotNull(systemMsg);
        Assert.Contains("financial", systemMsg!.Content!, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task FullCompactionPipeline_TriggerAndCompact()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        // Wire up the full pipeline
        var options = new CompactionOptions
        {
            TriggerMode = CompactionTriggerMode.TokenThreshold,
            Threshold = 50, // Low threshold to force trigger
            PreserveLastMessages = 2,
            PreserveSystemMessages = true,
            MinMessagesBeforeCompaction = 4,
            TargetCompressionRatio = 0.30
        };

        var trigger = new TokenThresholdTrigger(options);
        var strategy = new HierarchicalSummarizationStrategy();
        var kernel = OllamaConfig.CreateChatKernel();

        var history = new ChatHistory();
        history.AddSystemMessage("You are a helpful assistant.");

        // Build up history until trigger fires
        for (int i = 0; i < 10; i++)
        {
            history.AddUserMessage($"Question {i}: Can you explain concept number {i} in detail?");
            history.AddAssistantMessage($"Answer {i}: Concept {i} is an important topic that involves several key principles and practices.");
        }

        Assert.True(trigger.ShouldCompact(history), "Trigger should fire with accumulated history");

        var compacted = await strategy.CompactAsync(history, kernel, options);

        Assert.True(compacted.Count < history.Count);
        Assert.Contains(compacted, m => m.Role == AuthorRole.System);
    }
}
