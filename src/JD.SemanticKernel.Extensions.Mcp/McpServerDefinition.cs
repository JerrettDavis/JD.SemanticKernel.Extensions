using System;
using System.Collections.Generic;

namespace JD.SemanticKernel.Extensions.Mcp;

/// <summary>
/// Canonical representation of a discovered MCP server.
/// Normalizes configuration differences across all supported providers.
/// </summary>
public sealed class McpServerDefinition
{
    /// <summary>Gets the stable identity key used for caching and conflict resolution.</summary>
    public string Name { get; }

    /// <summary>Gets the human-readable display name of the server.</summary>
    public string DisplayName { get; }

    /// <summary>Gets the transport type for connecting to this server.</summary>
    public McpTransportType Transport { get; }

    /// <summary>Gets the configuration scope (Project &gt; User &gt; BuiltIn).</summary>
    public McpScope Scope { get; }

    /// <summary>Gets the identifier of the discovery provider that found this server.</summary>
    public string SourceProvider { get; }

    /// <summary>Gets the file path or source location where this definition originated, if applicable.</summary>
    public string? SourcePath { get; }

    /// <summary>Gets the HTTP endpoint URL (for <see cref="McpTransportType.Http"/> and <see cref="McpTransportType.WebSocket"/> transports).</summary>
    public Uri? Url { get; }

    /// <summary>Gets the command to launch for STDIO transport.</summary>
    public string? Command { get; }

    /// <summary>Gets the arguments to pass to the command for STDIO transport.</summary>
    public IReadOnlyList<string>? Args { get; }

    /// <summary>Gets the environment variables to set when launching the STDIO process.</summary>
    public IReadOnlyDictionary<string, string>? Env { get; }

    /// <summary>Gets a value indicating whether this server is enabled and should connect.</summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Initializes a new instance of <see cref="McpServerDefinition"/>.
    /// </summary>
    public McpServerDefinition(
        string name,
        string displayName,
        McpTransportType transport,
        McpScope scope,
        string sourceProvider,
        string? sourcePath = null,
        Uri? url = null,
        string? command = null,
        IReadOnlyList<string>? args = null,
        IReadOnlyDictionary<string, string>? env = null,
        bool isEnabled = true)
    {
#if NET8_0_OR_GREATER
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceProvider);
#else
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        if (string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(displayName));
        if (string.IsNullOrWhiteSpace(sourceProvider)) throw new ArgumentException("Value cannot be null or whitespace.", nameof(sourceProvider));
#endif

        Name = name;
        DisplayName = displayName;
        Transport = transport;
        Scope = scope;
        SourceProvider = sourceProvider;
        SourcePath = sourcePath;
        Url = url;
        Command = command;
        Args = args;
        Env = env;
        IsEnabled = isEnabled;
    }
}
