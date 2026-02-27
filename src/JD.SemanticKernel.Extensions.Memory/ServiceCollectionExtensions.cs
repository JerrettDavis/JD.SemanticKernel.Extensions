using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace JD.SemanticKernel.Extensions.Memory;

/// <summary>
/// Extension methods for registering semantic memory services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds semantic memory services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSemanticMemory(
        this IServiceCollection services,
        Action<SemanticMemoryOptions>? configure = null)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(services);
#else
        if (services is null) throw new ArgumentNullException(nameof(services));
#endif

        var options = new SemanticMemoryOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton(options.DefaultSearchOptions);

        // Register InMemoryBackend as default if no backend is registered
        services.TryAddSingleton<IMemoryBackend, InMemoryBackend>();

        // Register SemanticMemory
        services.AddSingleton<ISemanticMemory>(sp =>
        {
            var backend = sp.GetRequiredService<IMemoryBackend>();
            var kernel = sp.GetRequiredService<Microsoft.SemanticKernel.Kernel>();
            return new SemanticMemory(backend, kernel, options.DefaultSearchOptions);
        });

        return services;
    }
}
