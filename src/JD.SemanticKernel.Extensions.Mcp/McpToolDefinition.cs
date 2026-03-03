using System.Collections.Generic;

namespace JD.SemanticKernel.Extensions.Mcp;

/// <summary>
/// Describes a single tool exposed by an MCP server.
/// </summary>
public sealed class McpToolDefinition
{
    /// <summary>Gets the tool name as reported by the MCP server.</summary>
    public string Name { get; }

    /// <summary>Gets the human-readable description of the tool.</summary>
    public string? Description { get; }

    /// <summary>Gets the parameters accepted by this tool.</summary>
    public IReadOnlyList<McpToolParameter> Parameters { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="McpToolDefinition"/>.
    /// </summary>
    public McpToolDefinition(
        string name,
        string? description = null,
        IReadOnlyList<McpToolParameter>? parameters = null)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
#else
        if (string.IsNullOrWhiteSpace(name)) throw new System.ArgumentException("Value cannot be null or whitespace.", nameof(name));
#endif

        Name = name;
        Description = description;
        Parameters = parameters ?? System.Array.Empty<McpToolParameter>();
    }
}
