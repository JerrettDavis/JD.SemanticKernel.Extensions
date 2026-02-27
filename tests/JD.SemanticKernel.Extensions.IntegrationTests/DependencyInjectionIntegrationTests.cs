using JD.SemanticKernel.Extensions.Compaction;
using JD.SemanticKernel.Extensions.Memory;
using JD.SemanticKernel.Extensions.Memory.Sqlite;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Xunit;

namespace JD.SemanticKernel.Extensions.IntegrationTests;

/// <summary>
/// Integration tests validating the full DI pipeline —
/// service registration, resolution, and end-to-end execution.
/// </summary>
[Trait("Category", "Integration")]
public sealed class DependencyInjectionIntegrationTests : IDisposable
{
    private readonly string _dbPath;

    public DependencyInjectionIntegrationTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"sk-di-test-{Guid.NewGuid():N}.db");
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        try
        {
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
        catch (IOException)
        {
            // Best-effort cleanup
        }
    }

    [SkippableFact]
    public void AddCompaction_RegistersAllServices()
    {
        IntegrationGuard.EnsureEnabled();

        var services = new ServiceCollection();
        services.AddCompaction(opts =>
        {
            opts.TriggerMode = CompactionTriggerMode.TokenThreshold;
            opts.Threshold = 1000;
            opts.PreserveLastMessages = 5;
        });

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<ICompactionTrigger>());
        Assert.NotNull(provider.GetService<ICompactionStrategy>());
        Assert.NotNull(provider.GetService<CompactionOptions>());
        Assert.NotNull(provider.GetService<Microsoft.SemanticKernel.IAutoFunctionInvocationFilter>());

        var options = provider.GetRequiredService<CompactionOptions>();
        Assert.Equal(1000, options.Threshold);
        Assert.Equal(5, options.PreserveLastMessages);
    }

    [SkippableFact]
    public void AddSemanticMemory_WithInMemoryBackend_Resolves()
    {
        IntegrationGuard.EnsureEnabled();

        var services = new ServiceCollection();
        services.AddSingleton<IMemoryBackend, InMemoryBackend>();
        services.AddSingleton(OllamaConfig.CreateEmbeddingKernel());
        services.AddSemanticMemory();

        using var provider = services.BuildServiceProvider();

        var memory = provider.GetService<ISemanticMemory>();

        Assert.NotNull(memory);
        Assert.IsType<SemanticMemory>(memory);
    }

    [SkippableFact]
    public void AddSqliteMemoryBackend_Resolves()
    {
        IntegrationGuard.EnsureEnabled();

        var services = new ServiceCollection();
        services.AddSqliteMemoryBackendFromFile(_dbPath);

        using var provider = services.BuildServiceProvider();

        var backend = provider.GetService<IMemoryBackend>();

        Assert.NotNull(backend);
        Assert.IsType<SqliteMemoryBackend>(backend);
    }

    [SkippableFact]
    public async Task FullPipeline_DI_StoreAndSearch()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        var services = new ServiceCollection();
        services.AddSingleton(OllamaConfig.CreateEmbeddingKernel());
        services.AddSqliteMemoryBackendFromFile(_dbPath);
        services.AddSemanticMemory();

        using var provider = services.BuildServiceProvider();
        var memory = provider.GetRequiredService<ISemanticMemory>();

        // Full pipeline: store → search → verify
        await memory.StoreAsync("C# is a modern, object-oriented programming language.", id: "csharp-doc");
        await memory.StoreAsync("F# is a functional-first programming language for .NET.", id: "fsharp-doc");
        await memory.StoreAsync("Visual Basic .NET is used in many legacy enterprise applications.", id: "vb-doc");

        var results = await memory.SearchAsync("functional programming language",
            new MemorySearchOptions { TopK = 2, MinRelevanceScore = 0.1 });

        Assert.NotEmpty(results);
        // F# should be the top result for functional programming
        Assert.Equal("fsharp-doc", results[0].Record.Id);
    }

    [SkippableFact]
    public async Task FullPipeline_Compaction_TriggersAndCompacts()
    {
        IntegrationGuard.EnsureEnabled();
        Skip.IfNot(OllamaConfig.IsAvailable(), "Ollama not available");

        var services = new ServiceCollection();
        services.AddSingleton(OllamaConfig.CreateChatKernel());
        services.AddCompaction(opts =>
        {
            opts.TriggerMode = CompactionTriggerMode.TokenThreshold;
            opts.Threshold = 50;
            opts.PreserveLastMessages = 2;
            opts.MinMessagesBeforeCompaction = 4;
        });

        using var provider = services.BuildServiceProvider();
        var trigger = provider.GetRequiredService<ICompactionTrigger>();
        var strategy = provider.GetRequiredService<ICompactionStrategy>();
        var options = provider.GetRequiredService<CompactionOptions>();
        var kernel = provider.GetRequiredService<Kernel>();

        var history = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
        history.AddSystemMessage("You are helpful.");
        for (int i = 0; i < 10; i++)
        {
            history.AddUserMessage($"Tell me about topic {i} in great detail.");
            history.AddAssistantMessage($"Topic {i} is a very important subject with many facets to explore.");
        }

        Assert.True(trigger.ShouldCompact(history));

        var compacted = await strategy.CompactAsync(history, kernel, options);
        Assert.True(compacted.Count < history.Count);
    }
}
