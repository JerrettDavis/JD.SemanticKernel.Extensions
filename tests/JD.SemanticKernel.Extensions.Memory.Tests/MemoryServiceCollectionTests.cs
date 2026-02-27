using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Xunit;

namespace JD.SemanticKernel.Extensions.Memory.Tests;

public class MemoryServiceCollectionTests
{
    [Fact]
    public void AddSemanticMemory_RegistersOptions()
    {
        var services = new ServiceCollection();
        services.AddSemanticMemory();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<SemanticMemoryOptions>());
        Assert.NotNull(provider.GetService<MemorySearchOptions>());
    }

    [Fact]
    public void AddSemanticMemory_DefaultsToInMemoryBackend()
    {
        var services = new ServiceCollection();
        services.AddSemanticMemory();

        var provider = services.BuildServiceProvider();
        var backend = provider.GetRequiredService<IMemoryBackend>();

        Assert.IsType<InMemoryBackend>(backend);
    }

    [Fact]
    public void AddSemanticMemory_ConfiguresOptions()
    {
        var services = new ServiceCollection();
        services.AddSemanticMemory(opts =>
        {
            opts.DefaultSearchOptions.TopK = 25;
            opts.DefaultSearchOptions.MinRelevanceScore = 0.7;
        });

        var provider = services.BuildServiceProvider();
        var searchOptions = provider.GetRequiredService<MemorySearchOptions>();

        Assert.Equal(25, searchOptions.TopK);
        Assert.Equal(0.7, searchOptions.MinRelevanceScore);
    }

    [Fact]
    public void AddSemanticMemory_NullServices_Throws()
    {
        IServiceCollection services = null!;
        Assert.Throws<ArgumentNullException>(() => services.AddSemanticMemory());
    }

    [Fact]
    public void AddSemanticMemory_ReturnsServiceCollectionForChaining()
    {
        var services = new ServiceCollection();
        var result = services.AddSemanticMemory();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddSemanticMemory_DoesNotOverrideExistingBackend()
    {
        var services = new ServiceCollection();
        // Register a custom backend before AddSemanticMemory
        services.AddSingleton<IMemoryBackend>(new InMemoryBackend());
        services.AddSemanticMemory();

        var provider = services.BuildServiceProvider();
        var backend = provider.GetRequiredService<IMemoryBackend>();

        Assert.IsType<InMemoryBackend>(backend);
    }
}
