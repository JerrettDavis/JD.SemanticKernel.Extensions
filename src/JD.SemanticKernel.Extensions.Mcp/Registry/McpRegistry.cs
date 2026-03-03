using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JD.SemanticKernel.Extensions.Mcp.Registry;

/// <summary>
/// Merges all registered <see cref="IMcpDiscoveryProvider"/> instances,
/// applies scope-based precedence rules, and provides a unified view of
/// all available MCP servers.
/// </summary>
/// <remarks>
/// Precedence: <see cref="McpScope.Project"/> &gt; <see cref="McpScope.User"/> &gt; <see cref="McpScope.BuiltIn"/>.
/// When two providers report the same server name, the higher-scope definition wins.
/// </remarks>
public sealed class McpRegistry : IMcpRegistry
{
    private readonly IReadOnlyList<IMcpDiscoveryProvider> _providers;

    /// <summary>
    /// Initializes a new instance of <see cref="McpRegistry"/>.
    /// </summary>
    /// <param name="providers">The discovery providers to aggregate.</param>
    public McpRegistry(IReadOnlyList<IMcpDiscoveryProvider> providers)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(providers);
#else
        if (providers is null) throw new ArgumentNullException(nameof(providers));
#endif
        _providers = providers;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<McpServerDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var merged = new Dictionary<string, McpServerDefinition>(StringComparer.OrdinalIgnoreCase);

        foreach (var provider in _providers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var servers = await provider.DiscoverAsync(cancellationToken).ConfigureAwait(false);

            foreach (var server in servers)
            {
                if (!merged.TryGetValue(server.Name, out var existing) ||
                    server.Scope > existing.Scope)
                {
                    merged[server.Name] = server;
                }
            }
        }

        return new List<McpServerDefinition>(merged.Values).AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<McpServerDefinition?> GetAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
#else
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
#endif

        var all = await GetAllAsync(cancellationToken).ConfigureAwait(false);
        foreach (var server in all)
        {
            if (string.Equals(server.Name, name, StringComparison.OrdinalIgnoreCase))
                return server;
        }

        return null;
    }
}
