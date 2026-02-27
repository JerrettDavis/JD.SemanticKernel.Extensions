using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Xunit;

namespace JD.SemanticKernel.Extensions.Compaction.Tests;

public class CompactionServiceCollectionTests
{
    [Fact]
    public void AddCompaction_RegistersAllServices()
    {
        var services = new ServiceCollection();
        services.AddCompaction();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<CompactionOptions>());
        Assert.NotNull(provider.GetService<ICompactionTrigger>());
        Assert.NotNull(provider.GetService<ICompactionStrategy>());
        Assert.NotNull(provider.GetService<IAutoFunctionInvocationFilter>());
    }

    [Fact]
    public void AddCompaction_DefaultOptions_UsesContextPercentageTrigger()
    {
        var services = new ServiceCollection();
        services.AddCompaction();

        var provider = services.BuildServiceProvider();
        var trigger = provider.GetRequiredService<ICompactionTrigger>();

        Assert.IsType<ContextPercentageTrigger>(trigger);
    }

    [Fact]
    public void AddCompaction_TokenThresholdMode_UsesTokenThresholdTrigger()
    {
        var services = new ServiceCollection();
        services.AddCompaction(opts =>
        {
            opts.TriggerMode = CompactionTriggerMode.TokenThreshold;
            opts.Threshold = 50_000;
        });

        var provider = services.BuildServiceProvider();
        var trigger = provider.GetRequiredService<ICompactionTrigger>();

        Assert.IsType<TokenThresholdTrigger>(trigger);
    }

    [Fact]
    public void AddCompaction_ConfiguresOptions()
    {
        var services = new ServiceCollection();
        services.AddCompaction(opts =>
        {
            opts.PreserveLastMessages = 20;
            opts.Threshold = 0.85;
            opts.MaxContextWindowTokens = 200_000;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<CompactionOptions>();

        Assert.Equal(20, options.PreserveLastMessages);
        Assert.Equal(0.85, options.Threshold);
        Assert.Equal(200_000, options.MaxContextWindowTokens);
    }

    [Fact]
    public void AddCompaction_RegistersStrategy_AsHierarchicalSummarization()
    {
        var services = new ServiceCollection();
        services.AddCompaction();

        var provider = services.BuildServiceProvider();
        var strategy = provider.GetRequiredService<ICompactionStrategy>();

        Assert.IsType<HierarchicalSummarizationStrategy>(strategy);
    }

    [Fact]
    public void AddCompaction_NullServices_Throws()
    {
        IServiceCollection services = null!;
        Assert.Throws<ArgumentNullException>(() => services.AddCompaction());
    }

    [Fact]
    public void AddCompaction_ReturnsServiceCollectionForChaining()
    {
        var services = new ServiceCollection();
        var result = services.AddCompaction();

        Assert.Same(services, result);
    }
}
