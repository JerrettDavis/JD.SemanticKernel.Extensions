using JD.SemanticKernel.Extensions.Memory;
using JD.SemanticKernel.Extensions.Memory.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.SemanticKernel;
using Xunit;

namespace JD.SemanticKernel.Extensions.IntegrationTests;

/// <summary>
/// Integration tests for SQLite memory backend with real embeddings.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SqliteMemoryIntegrationTests : IDisposable
{
    private readonly string _dbPath;
    private readonly SqliteMemoryBackend _backend;

    public SqliteMemoryIntegrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"sk-memory-test-{Guid.NewGuid():N}.db");
        _backend = SqliteMemoryBackend.FromFile(_dbPath);
    }

    public void Dispose()
    {
        _backend.Dispose();
        SqliteConnection.ClearAllPools();

        // Give SQLite a moment to fully release the file
        GC.Collect();
        GC.WaitForPendingFinalizers();

        try
        {
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
        catch (IOException)
        {
            // Best-effort cleanup; temp directory will be cleaned eventually
        }
    }

    [SkippableFact]
    public async Task SqliteBackend_StoreAndRetrieve_Persists()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        var kernel = OllamaConfig.CreateEmbeddingKernel();
        var memory = new SemanticMemory(_backend, kernel);

        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["source"] = "integration-test",
            ["type"] = "database"
        };

        var id = await memory.StoreAsync("SQLite is a lightweight embedded database.",
            metadata);

        Assert.True(await memory.ExistsAsync(id));

        // Search should find the stored record
        var results = await memory.SearchAsync("embedded database engine",
            new MemorySearchOptions { TopK = 1, MinRelevanceScore = 0.1 });

        Assert.Single(results);
        Assert.Equal(id, results[0].Record.Id);
        Assert.Contains("SQLite", results[0].Record.Text, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task SqliteBackend_PersistsAcrossInstances()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        var kernel = OllamaConfig.CreateEmbeddingKernel();

        // Store with first instance
        const string DocId = "persist-test-001";
        var memory1 = new SemanticMemory(_backend, kernel);
        await memory1.StoreAsync("Data that should persist across connections.", id: DocId);

        // Dispose and clear pool before recreating
        _backend.Dispose();
        SqliteConnection.ClearAllPools();

        using var backend2 = SqliteMemoryBackend.FromFile(_dbPath);
        var memory2 = new SemanticMemory(backend2, kernel);

        Assert.True(await memory2.ExistsAsync(DocId));
    }

    [SkippableFact]
    public async Task SqliteBackend_MultipleDocuments_SearchRanks()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        var kernel = OllamaConfig.CreateEmbeddingKernel();
        var memory = new SemanticMemory(_backend, kernel);

        await memory.StoreAsync("PostgreSQL is an advanced open-source relational database.");
        await memory.StoreAsync("Redis is an in-memory data structure store used as a cache.");
        await memory.StoreAsync("MongoDB is a document-oriented NoSQL database.");
        await memory.StoreAsync("Elasticsearch is a distributed search and analytics engine.");

        var results = await memory.SearchAsync("NoSQL document database",
            new MemorySearchOptions { TopK = 2, MinRelevanceScore = 0.1 });

        Assert.NotEmpty(results);
        // MongoDB should rank high for a NoSQL query
        Assert.Contains(results, r =>
            r.Record.Text.Contains("MongoDB", StringComparison.OrdinalIgnoreCase));
    }

    [SkippableFact]
    public async Task SqliteBackend_Delete_RemovesPersisted()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        var kernel = OllamaConfig.CreateEmbeddingKernel();
        var memory = new SemanticMemory(_backend, kernel);

        var id = await memory.StoreAsync("This document will be deleted.");

        Assert.True(await memory.ExistsAsync(id));

        await memory.ForgetAsync(id);

        Assert.False(await memory.ExistsAsync(id));
    }

    [SkippableFact]
    public async Task SqliteBackend_MetadataPreserved()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        var kernel = OllamaConfig.CreateEmbeddingKernel();
        var memory = new SemanticMemory(_backend, kernel);

        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["source"] = "test-suite",
            ["priority"] = "high"
        };

        var id = await memory.StoreAsync(
            "Metadata should survive SQLite round-trip.",
            metadata,
            "meta-test-001");

        var results = await memory.SearchAsync("metadata round trip",
            new MemorySearchOptions { TopK = 1, MinRelevanceScore = 0.1 });

        Assert.Single(results);
        Assert.NotNull(results[0].Record.Metadata);
        Assert.Equal("test-suite", results[0].Record.Metadata["source"]);
        Assert.Equal("high", results[0].Record.Metadata["priority"]);
    }
}
