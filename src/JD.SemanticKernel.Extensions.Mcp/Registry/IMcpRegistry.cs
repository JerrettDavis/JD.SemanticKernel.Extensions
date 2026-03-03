using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JD.SemanticKernel.Extensions.Mcp;

/// <summary>
/// Merges all registered <see cref="IMcpDiscoveryProvider"/> instances, applies precedence rules,
/// and provides a unified view of all available MCP servers.
/// </summary>
public interface IMcpRegistry
{
    /// <summary>
    /// Returns all known MCP server definitions after applying precedence/conflict resolution.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of merged server definitions.</returns>
    Task<IReadOnlyList<McpServerDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the server definition for the given name, or <c>null</c> if not found.
    /// </summary>
    /// <param name="name">The stable identity key of the server.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task<McpServerDefinition?> GetAsync(
        string name,
        CancellationToken cancellationToken = default);
}
