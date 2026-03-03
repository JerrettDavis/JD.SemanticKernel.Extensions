using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace JD.SemanticKernel.Extensions.Mcp.Discovery;

/// <summary>
/// Discovers MCP servers from Claude Code configuration files.
/// Reads <c>~/.claude.json</c> and any workspace-local <c>.mcp.json</c> file.
/// </summary>
public sealed class ClaudeCodeMcpDiscoveryProvider : FileMcpDiscoveryProvider
{
    private readonly string? _workingDirectory;

    /// <summary>
    /// Initializes a new instance of <see cref="ClaudeCodeMcpDiscoveryProvider"/>.
    /// </summary>
    /// <param name="workingDirectory">Optional workspace root; defaults to <see cref="Directory.GetCurrentDirectory"/>.</param>
    public ClaudeCodeMcpDiscoveryProvider(string? workingDirectory = null)
    {
        _workingDirectory = workingDirectory;
    }

    /// <inheritdoc/>
    public override string ProviderId => "claude-code";

    /// <inheritdoc/>
    protected override IEnumerable<string> GetConfigFilePaths()
    {
        // User-level config
        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        yield return Path.Combine(userHome, ".claude.json");

        // Workspace-level config
        var workDir = _workingDirectory ?? Directory.GetCurrentDirectory();
        yield return Path.Combine(workDir, ".mcp.json");
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<McpServerDefinition> ParseConfig(string json, string sourcePath)
    {
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });

        var scope = sourcePath.EndsWith(".mcp.json", StringComparison.OrdinalIgnoreCase)
            ? McpScope.Project
            : McpScope.User;

        return McpConfigParser.ParseMcpServers(doc.RootElement, ProviderId, sourcePath, scope);
    }
}
