using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JD.SemanticKernel.Extensions.Mcp;

/// <summary>
/// Discovers MCP server definitions from a specific provider ecosystem.
/// </summary>
public interface IMcpDiscoveryProvider
{
    /// <summary>Gets the unique identifier for this provider (e.g., "claude-code", "vscode").</summary>
    string ProviderId { get; }

    /// <summary>
    /// Discovers all MCP servers available from this provider.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of discovered server definitions.</returns>
    Task<IReadOnlyList<McpServerDefinition>> DiscoverAsync(
        CancellationToken cancellationToken = default);
}
