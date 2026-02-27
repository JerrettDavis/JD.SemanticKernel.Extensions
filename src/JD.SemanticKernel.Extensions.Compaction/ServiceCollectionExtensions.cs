using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace JD.SemanticKernel.Extensions.Compaction;

/// <summary>
/// Extension methods for registering compaction services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds conversation compaction middleware to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCompaction(
        this IServiceCollection services,
        Action<CompactionOptions>? configure = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(services);
#else
        if (services is null) throw new ArgumentNullException(nameof(services));
#endif

        var options = new CompactionOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);

        // Register trigger based on mode
        switch (options.TriggerMode)
        {
            case CompactionTriggerMode.TokenThreshold:
                services.AddSingleton<ICompactionTrigger>(new TokenThresholdTrigger(options));
                break;
            case CompactionTriggerMode.ContextPercentage:
            default:
                services.AddSingleton<ICompactionTrigger>(new ContextPercentageTrigger(options));
                break;
        }

        // Register default strategy
        services.AddSingleton<ICompactionStrategy, HierarchicalSummarizationStrategy>();

        // Register the filter
        services.AddSingleton<IAutoFunctionInvocationFilter, CompactionFilter>();

        return services;
    }
}
