using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace JD.SemanticKernel.Extensions.Mcp.Discovery;

/// <summary>
/// Discovers MCP servers from OpenAI Codex configuration.
/// Reads <c>~/.codex/config.json</c> and workspace-local <c>.codex/config.json</c>.
/// </summary>
public sealed class CodexMcpDiscoveryProvider : FileMcpDiscoveryProvider
{
    private readonly string? _workingDirectory;

    /// <summary>
    /// Initializes a new instance of <see cref="CodexMcpDiscoveryProvider"/>.
    /// </summary>
    /// <param name="workingDirectory">Optional workspace root; defaults to <see cref="Directory.GetCurrentDirectory"/>.</param>
    public CodexMcpDiscoveryProvider(string? workingDirectory = null)
    {
        _workingDirectory = workingDirectory;
    }

    /// <inheritdoc/>
    public override string ProviderId => "codex";

    /// <inheritdoc/>
    protected override IEnumerable<string> GetConfigFilePaths()
    {
        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        yield return Path.Combine(userHome, ".codex", "config.json");

        var workDir = _workingDirectory ?? Directory.GetCurrentDirectory();
        yield return Path.Combine(workDir, ".codex", "config.json");
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<McpServerDefinition> ParseConfig(string json, string sourcePath)
    {
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });

        var workDir = _workingDirectory ?? Directory.GetCurrentDirectory();
        var scope = sourcePath.StartsWith(
            workDir, StringComparison.OrdinalIgnoreCase)
            ? McpScope.Project
            : McpScope.User;

        return McpConfigParser.ParseMcpServers(doc.RootElement, ProviderId, sourcePath, scope);
    }
}
