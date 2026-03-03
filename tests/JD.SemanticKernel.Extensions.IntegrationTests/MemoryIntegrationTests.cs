using JD.SemanticKernel.Extensions.Memory;
using Microsoft.SemanticKernel;
using Xunit;

namespace JD.SemanticKernel.Extensions.IntegrationTests;

/// <summary>
/// Integration tests for SemanticMemory using real embeddings from Ollama.
/// </summary>
[Trait("Category", "Integration")]
public sealed class MemoryIntegrationTests : IDisposable
{
    private readonly InMemoryBackend _backend = new();

    public void Dispose()
    {
        // InMemoryBackend has no resources to release
    }

    [SkippableFact]
    public async Task StoreAndSearch_WithRealEmbeddings_FindsRelevantResult()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        var kernel = OllamaConfig.CreateEmbeddingKernel();
        var memory = new SemanticMemory(_backend, kernel);

        await memory.StoreAsync("Cats are small domesticated mammals that are often kept as pets.");
        await memory.StoreAsync("Dogs are loyal companions and come in many different breeds.");
        await memory.StoreAsync("Python is a popular programming language used for machine learning.");
        await memory.StoreAsync("JavaScript runs in web browsers and powers interactive websites.");

        var results = await memory.SearchAsync("What programming language is used for AI?",
            new MemorySearchOptions { TopK = 2, MinRelevanceScore = 0.1 });

        Assert.NotEmpty(results);

        // Python should be the top result for an AI/ML query
        var topResult = results[0];
        Assert.Contains("Python", topResult.Record.Text, StringComparison.OrdinalIgnoreCase);
        Assert.True(topResult.RelevanceScore > 0, "Score should be positive for relevant results");
    }

    [SkippableFact]
    public async Task StoreAndSearch_SemanticallyRelated_RankedBySimilarity()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        var kernel = OllamaConfig.CreateEmbeddingKernel();
        var memory = new SemanticMemory(_backend, kernel);

        await memory.StoreAsync("The weather today is sunny with clear skies.");
        await memory.StoreAsync("Artificial intelligence is transforming healthcare diagnostics.");
        await memory.StoreAsync("Machine learning models require large datasets for training.");
        await memory.StoreAsync("The recipe calls for two cups of flour and one egg.");

        var results = await memory.SearchAsync("deep learning and neural networks",
            new MemorySearchOptions { TopK = 4, MinRelevanceScore = 0.1 });

        Assert.NotEmpty(results);

        // AI/ML content should rank higher than weather or recipes
        var topTwo = results.Take(2).Select(r => r.Record.Text).ToList();
        var hasRelevant = topTwo.Any(t =>
            t.Contains("artificial", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("machine", StringComparison.OrdinalIgnoreCase));

        Assert.True(hasRelevant,
            $"Expected AI/ML content in top 2 results. Got: {string.Join(" | ", topTwo)}");
    }

    [SkippableFact]
    public async Task Store_WithMetadata_PreservesMetadata()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        var kernel = OllamaConfig.CreateEmbeddingKernel();
        var memory = new SemanticMemory(_backend, kernel);

        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["source"] = "integration-test",
            ["category"] = "science"
        };

        var id = await memory.StoreAsync(
            "Quantum computing uses qubits instead of classical bits.",
            metadata);

        Assert.False(string.IsNullOrWhiteSpace(id));

        var results = await memory.SearchAsync("quantum computers",
            new MemorySearchOptions { TopK = 1, MinRelevanceScore = 0.1 });

        Assert.Single(results);
        Assert.Equal(id, results[0].Record.Id);
        Assert.NotNull(results[0].Record.Metadata);
        Assert.Equal("science", results[0].Record.Metadata["category"]);
    }

    [SkippableFact]
    public async Task Store_WithExplicitId_CanRetrieveById()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        var kernel = OllamaConfig.CreateEmbeddingKernel();
        var memory = new SemanticMemory(_backend, kernel);

        const string CustomId = "test-doc-001";
        await memory.StoreAsync("The Eiffel Tower is located in Paris, France.", id: CustomId);

        Assert.True(await memory.ExistsAsync(CustomId));
    }

    [SkippableFact]
    public async Task Forget_RemovesDocument()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        var kernel = OllamaConfig.CreateEmbeddingKernel();
        var memory = new SemanticMemory(_backend, kernel);

        var id = await memory.StoreAsync("Temporary data that should be removed.");

        Assert.True(await memory.ExistsAsync(id));

        await memory.ForgetAsync(id);

        Assert.False(await memory.ExistsAsync(id));
    }

    [SkippableFact]
    public async Task Search_EmptyStore_ReturnsEmpty()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        var kernel = OllamaConfig.CreateEmbeddingKernel();
        var emptyBackend = new InMemoryBackend();
        var memory = new SemanticMemory(emptyBackend, kernel);

        var results = await memory.SearchAsync("anything");

        Assert.Empty(results);
    }

    [SkippableFact]
    public async Task EmbeddingService_ProducesNonZeroVectors()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        var kernel = OllamaConfig.CreateEmbeddingKernel();

#pragma warning disable SKEXP0010, CS0618
        var embeddingService = kernel.GetRequiredService<Microsoft.SemanticKernel.Embeddings.ITextEmbeddingGenerationService>();

        var embeddings = await embeddingService.GenerateEmbeddingsAsync(
            ["Hello world", "Semantic search test"]);
#pragma warning restore SKEXP0010, CS0618

        Assert.Equal(2, embeddings.Count);
        Assert.True(embeddings[0].Length > 0, "Embedding should have non-zero dimensions");
        Assert.True(embeddings[1].Length > 0, "Embedding should have non-zero dimensions");

        // Embeddings should not be all zeros
        Assert.Contains(embeddings[0].ToArray(), v => Math.Abs(v) > 0.001f);
    }
}
