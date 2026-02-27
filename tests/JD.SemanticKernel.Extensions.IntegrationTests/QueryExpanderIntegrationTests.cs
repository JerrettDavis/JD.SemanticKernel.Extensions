using JD.SemanticKernel.Extensions.Memory;
using Microsoft.SemanticKernel;
using Xunit;

namespace JD.SemanticKernel.Extensions.IntegrationTests;

/// <summary>
/// Integration tests for QueryExpander using a real LLM (Ollama).
/// </summary>
[Trait("Category", "Integration")]
public sealed class QueryExpanderIntegrationTests
{
    [SkippableFact]
    public async Task ExpandAsync_ProducesMultipleVariants()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        var kernel = OllamaConfig.CreateChatKernel();
        var expander = new QueryExpander();

        var expanded = await expander.ExpandAsync("What is dependency injection?", kernel);

        // Should produce the original query plus at least one expansion
        Assert.True(expanded.Count >= 2,
            $"Expected at least 2 query variants, got {expanded.Count}: [{string.Join(", ", expanded)}]");

        // Original should be included
        Assert.Contains(expanded, q =>
            q.Contains("dependency injection", StringComparison.OrdinalIgnoreCase));
    }

    [SkippableFact]
    public async Task ExpandAsync_VariantsAreSemanticallyRelated()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        var kernel = OllamaConfig.CreateChatKernel();
        var expander = new QueryExpander();

        var expanded = await expander.ExpandAsync("How to optimize SQL queries?", kernel);

        Assert.True(expanded.Count >= 2);

        // Variants should contain query-related terms
        var allText = string.Join(" ", expanded).ToUpperInvariant();
        var hasRelevantTerm = allText.Contains("SQL", StringComparison.Ordinal) ||
                              allText.Contains("QUERY", StringComparison.Ordinal) ||
                              allText.Contains("DATABASE", StringComparison.Ordinal) ||
                              allText.Contains("PERFORMANCE", StringComparison.Ordinal) ||
                              allText.Contains("OPTIM", StringComparison.Ordinal);

        Assert.True(hasRelevantTerm,
            $"Expected SQL-related terms in expanded queries: {allText}");
    }

    [SkippableFact]
    public async Task ExpandAsync_WithMemorySearch_ImprovesRecall()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        var kernel = OllamaConfig.CreateFullKernel();
        var backend = new InMemoryBackend();
        var memory = new SemanticMemory(backend, kernel);

        // Store documents with varied terminology
        await memory.StoreAsync("The SOLID principles guide object-oriented software design.");
        await memory.StoreAsync("Inversion of Control containers manage object lifecycle and dependencies.");
        await memory.StoreAsync("Service locator is an anti-pattern compared to constructor injection.");
        await memory.StoreAsync("The weather in Seattle is often rainy during winter months.");

        // Search with single query
        var directResults = await memory.SearchAsync("dependency injection",
            new MemorySearchOptions { TopK = 4, MinRelevanceScore = 0.1 });

        // Search with expanded queries and merge results
        var expander = new QueryExpander();
        var expandedQueries = await expander.ExpandAsync("dependency injection", kernel);

        var allExpandedResults = new List<MemoryResult>();
        foreach (var query in expandedQueries)
        {
            var results = await memory.SearchAsync(query,
                new MemorySearchOptions { TopK = 4, MinRelevanceScore = 0.1 });
            allExpandedResults.AddRange(results);
        }

        // Deduplicate by ID
        var uniqueExpanded = allExpandedResults
            .GroupBy(r => r.Record.Id, StringComparer.Ordinal)
            .Select(g => g.OrderByDescending(r => r.RelevanceScore).First())
            .ToList();

        // Expanded queries should find at least as many relevant results
        Assert.True(uniqueExpanded.Count >= directResults.Count,
            $"Expanded search ({uniqueExpanded.Count}) should find >= direct search ({directResults.Count})");
    }
}
