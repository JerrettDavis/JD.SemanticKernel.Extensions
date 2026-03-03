using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace JD.SemanticKernel.Extensions.Mcp.Discovery;

/// <summary>
/// Discovers MCP servers from the Claude Desktop application configuration.
/// Reads the platform-specific <c>claude_desktop_config.json</c> file.
/// </summary>
public sealed class ClaudeDesktopMcpDiscoveryProvider : FileMcpDiscoveryProvider
{
    /// <inheritdoc/>
    public override string ProviderId => "claude-desktop";

    /// <inheritdoc/>
    protected override IEnumerable<string> GetConfigFilePaths()
    {
        var path = GetClaudeDesktopConfigPath();
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

    private static string? GetClaudeDesktopConfigPath()
    {
#if NET8_0_OR_GREATER
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "Claude", "claude_desktop_config.json");
        }

        if (OperatingSystem.IsMacOS())
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(userProfile, "Library", "Application Support", "Claude", "claude_desktop_config.json");
        }

        if (OperatingSystem.IsLinux())
        {
            var configHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
            return Path.Combine(configHome, "Claude", "claude_desktop_config.json");
        }
#else
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (!string.IsNullOrEmpty(appData))
            return Path.Combine(appData, "Claude", "claude_desktop_config.json");

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrEmpty(userProfile))
            return Path.Combine(userProfile, "Library", "Application Support", "Claude", "claude_desktop_config.json");
#endif
        return null;
    }
}
