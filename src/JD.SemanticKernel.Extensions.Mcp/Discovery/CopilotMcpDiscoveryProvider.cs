using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace JD.SemanticKernel.Extensions.Mcp.Discovery;

/// <summary>
/// Discovers MCP servers from Microsoft Copilot configuration.
/// Reads the platform-specific Copilot MCP config file.
/// </summary>
public sealed class CopilotMcpDiscoveryProvider : FileMcpDiscoveryProvider
{
    /// <inheritdoc/>
    public override string ProviderId => "copilot";

    /// <inheritdoc/>
    protected override IEnumerable<string> GetConfigFilePaths()
    {
        var path = GetCopilotConfigPath();
        if (path is not null)
            yield return path;
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<McpServerDefinition> ParseConfig(string json, string sourcePath)
    {
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });

        return McpConfigParser.ParseMcpServers(doc.RootElement, ProviderId, sourcePath, McpScope.User);
    }

    private static string? GetCopilotConfigPath()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(userProfile))
            return null;

#if NET8_0_OR_GREATER
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "GitHub Copilot", "mcp.json");
        }

        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(userProfile, "Library", "Application Support", "GitHub Copilot", "mcp.json");
        }

        return Path.Combine(userProfile, ".config", "github-copilot", "mcp.json");
#else
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (!string.IsNullOrEmpty(appData))
            return Path.Combine(appData, "GitHub Copilot", "mcp.json");

        return Path.Combine(userProfile, ".config", "github-copilot", "mcp.json");
#endif
    }
}
