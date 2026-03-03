using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace JD.SemanticKernel.Extensions.Mcp.Discovery;

/// <summary>
/// Discovers MCP servers from the JD canonical configuration file (<c>jdai.mcp.json</c>).
/// Reads user-level and project-level files.
/// </summary>
public sealed class JdCanonicalMcpDiscoveryProvider : FileMcpDiscoveryProvider
{
    private readonly string? _workingDirectory;

    /// <summary>
    /// Initializes a new instance of <see cref="JdCanonicalMcpDiscoveryProvider"/>.
    /// </summary>
    /// <param name="workingDirectory">Optional workspace root; defaults to <see cref="Directory.GetCurrentDirectory"/>.</param>
    public JdCanonicalMcpDiscoveryProvider(string? workingDirectory = null)
    {
        _workingDirectory = workingDirectory;
    }

    /// <inheritdoc/>
    public override string ProviderId => "jd-canonical";

    /// <inheritdoc/>
    protected override IEnumerable<string> GetConfigFilePaths()
    {
        // User-level
        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        yield return Path.Combine(userHome, ".jdai", "jdai.mcp.json");

        // Project-level (highest priority)
        var workDir = _workingDirectory ?? Directory.GetCurrentDirectory();
        yield return Path.Combine(workDir, "jdai.mcp.json");
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<McpServerDefinition> ParseConfig(string json, string sourcePath)
    {
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });

        var scope = IsProjectPath(sourcePath) ? McpScope.Project : McpScope.User;

        return McpConfigParser.ParseMcpServers(doc.RootElement, ProviderId, sourcePath, scope);
    }

    private bool IsProjectPath(string sourcePath)
    {
        var workDir = _workingDirectory ?? Directory.GetCurrentDirectory();
        return sourcePath.StartsWith(workDir, StringComparison.OrdinalIgnoreCase);
    }
}
