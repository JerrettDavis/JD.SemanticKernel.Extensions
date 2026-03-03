namespace JD.SemanticKernel.Extensions.Mcp;

/// <summary>
/// Specifies the transport mechanism used to communicate with an MCP server.
/// </summary>
public enum McpTransportType
{
    /// <summary>Standard input/output transport.</summary>
    Stdio,

    /// <summary>HTTP (JSON-RPC) transport.</summary>
    Http,

    /// <summary>WebSocket transport (reserved for future use).</summary>
    WebSocket,
}
