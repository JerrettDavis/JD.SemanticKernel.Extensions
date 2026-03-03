using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace JD.SemanticKernel.Extensions.Mcp.Discovery;

/// <summary>
/// Discovers MCP servers from VS Code Copilot configuration.
/// Reads workspace-level <c>.vscode/mcp.json</c> and user-level VS Code settings.
/// </summary>
public sealed class VsCodeMcpDiscoveryProvider : FileMcpDiscoveryProvider
{
    private readonly string? _workspaceRoot;

    /// <summary>
    /// Initializes a new instance of <see cref="VsCodeMcpDiscoveryProvider"/>.
    /// </summary>
    /// <param name="workspaceRoot">Optional workspace root directory; defaults to <see cref="Directory.GetCurrentDirectory"/>.</param>
    public VsCodeMcpDiscoveryProvider(string? workspaceRoot = null)
    {
        _workspaceRoot = workspaceRoot;
    }

    /// <inheritdoc/>
    public override string ProviderId => "vscode";

    /// <inheritdoc/>
    protected override IEnumerable<string> GetConfigFilePaths()
    {
        // Workspace-level
        var workDir = _workspaceRoot ?? Directory.GetCurrentDirectory();
        yield return Path.Combine(workDir, ".vscode", "mcp.json");

        // User-level VS Code settings directory
        var userSettingsDir = GetVsCodeUserSettingsDir();
        if (userSettingsDir is not null)
            yield return Path.Combine(userSettingsDir, "mcp.json");
    }

    /// <inheritdoc/>
    protected override IReadOnlyList<McpServerDefinition> ParseConfig(string json, string sourcePath)
    {
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        });

        var scope = sourcePath.Contains(".vscode", StringComparison.OrdinalIgnoreCase)
            ? McpScope.Project
            : McpScope.User;

        // VS Code mcp.json may use "servers" instead of "mcpServers"
        var root = doc.RootElement;

        // Try the standard mcpServers key first
        var results = McpConfigParser.ParseMcpServers(root, ProviderId, sourcePath, scope);

        if (results.Count == 0 && root.TryGetProperty("servers", out var serversEl)
            && serversEl.ValueKind == JsonValueKind.Object)
        {
            var list = new List<McpServerDefinition>();
            foreach (var serverProp in serversEl.EnumerateObject())
            {
                var def = McpConfigParser.ParseServerEntry(
                    serverProp.Name, serverProp.Value, ProviderId, sourcePath, scope);
                if (def is not null)
                    list.Add(def);
            }

            return list;
        }

        return results;
    }

    private static string? GetVsCodeUserSettingsDir()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(userProfile))
            return null;

        // Windows: %APPDATA%\Code\User
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var windowsPath = Path.Combine(appData, "Code", "User");
        if (Directory.Exists(windowsPath))
            return windowsPath;

        // macOS: ~/Library/Application Support/Code/User
        var macPath = Path.Combine(userProfile, "Library", "Application Support", "Code", "User");
        if (Directory.Exists(macPath))
            return macPath;

        // Linux: ~/.config/Code/User
        var linuxPath = Path.Combine(userProfile, ".config", "Code", "User");
        if (Directory.Exists(linuxPath))
            return linuxPath;

        return null;
    }
}
