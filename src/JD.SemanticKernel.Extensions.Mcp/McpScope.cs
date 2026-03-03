namespace JD.SemanticKernel.Extensions.Mcp;

/// <summary>
/// Indicates the scope at which an MCP server is configured.
/// Higher-priority scopes override lower-priority ones during conflict resolution.
/// </summary>
public enum McpScope
{
    /// <summary>Built-in or global scope (lowest priority).</summary>
    BuiltIn = 0,

    /// <summary>User-level configuration.</summary>
    User = 1,

    /// <summary>Project-level configuration (highest priority).</summary>
    Project = 2,
}
