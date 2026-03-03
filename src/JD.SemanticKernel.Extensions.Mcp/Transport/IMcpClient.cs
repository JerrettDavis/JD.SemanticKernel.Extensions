using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace JD.SemanticKernel.Extensions.Mcp;

/// <summary>
/// Connects to a single MCP server and exposes its tools for invocation.
/// </summary>
public interface IMcpClient
{
    /// <summary>
    /// Performs the MCP initialization handshake with the server.
    /// Must be called before <see cref="GetToolsAsync"/> or <see cref="InvokeAsync"/>.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the list of tools exposed by the server.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of tool definitions.</returns>
    Task<IReadOnlyList<McpToolDefinition>> GetToolsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invokes a tool by name with the provided arguments.
    /// </summary>
    /// <param name="toolName">The name of the tool to invoke.</param>
    /// <param name="arguments">Named arguments to pass to the tool.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The result of the invocation.</returns>
    Task<McpInvocationResult> InvokeAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default);
}
