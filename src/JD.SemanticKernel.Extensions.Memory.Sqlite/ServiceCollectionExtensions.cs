using System;
using Microsoft.Extensions.DependencyInjection;
using JD.SemanticKernel.Extensions.Memory;

namespace JD.SemanticKernel.Extensions.Memory.Sqlite;

/// <summary>
/// Extension methods for registering the SQLite memory backend.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SQLite memory backend.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">SQLite connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSqliteMemoryBackend(
        this IServiceCollection services,
        string connectionString)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(services);
#else
        if (services is null) throw new ArgumentNullException(nameof(services));
#endif

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string cannot be null or whitespace.", nameof(connectionString));
        }

        services.AddSingleton<IMemoryBackend>(new SqliteMemoryBackend(connectionString));
        return services;
    }

    /// <summary>
    /// Registers the SQLite memory backend with a file path.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="databasePath">Path to the SQLite database file.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddSqliteMemoryBackendFromFile(
        this IServiceCollection services,
        string databasePath)
    {
        return services.AddSqliteMemoryBackend($"Data Source={databasePath}");
    }
}
