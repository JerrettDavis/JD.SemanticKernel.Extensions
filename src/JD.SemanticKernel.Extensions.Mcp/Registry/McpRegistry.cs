using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PatternKit.Behavioral.Chain;

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
        var state = new McpDiscoveryState(merged);
        var chainBuilder = AsyncActionChain<McpDiscoveryState>.Create();

        foreach (var provider in _providers)
        {
            chainBuilder.Use(async (current, token, next) =>
            {
                token.ThrowIfCancellationRequested();
                var servers = await provider.DiscoverAsync(token).ConfigureAwait(false);
                current.Merge(servers);
                await next(current, token).ConfigureAwait(false);
            });
        }

        var chain = chainBuilder.Build();
        await chain.ExecuteAsync(state, cancellationToken).ConfigureAwait(false);

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

    private sealed class McpDiscoveryState
    {
        private readonly Dictionary<string, McpServerDefinition> _merged;

        public McpDiscoveryState(Dictionary<string, McpServerDefinition> merged)
        {
            _merged = merged;
        }

        public void Merge(IEnumerable<McpServerDefinition> servers)
        {
            foreach (var server in servers)
            {
                if (!_merged.TryGetValue(server.Name, out var existing) ||
                    server.Scope > existing.Scope)
                {
                    _merged[server.Name] = server;
                }
            }
        }
    }
}
